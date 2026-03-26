using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using server_dotnet.Constants;
using server_dotnet.Data;
using server_dotnet.DTOs;
using BCrypt.Net;
using Yitter.IdGenerator;

namespace server_dotnet.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class AuthController : BaseController
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("register")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest request)
    {
        // Validation is handled by model validation, but we check for basic empty values
        if (string.IsNullOrWhiteSpace(request.Username) ||
            string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { error = "请填写完整信息" });
        }

        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (existingUser != null)
        {
            return BadRequest(new { error = "邮箱已被注册" });
        }

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password, BCrypt.Net.BCrypt.GenerateSalt(10));

        var user = new server_dotnet.Models.User
        {
            Id = YitIdHelper.NextId(),
            Username = request.Username,
            Email = request.Email,
            Password = hashedPassword,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // 注册时设置 CreatedBy 和 UpdatedBy
        user.CreatedBy = user.Id;
        user.UpdatedBy = user.Id;
        await _context.SaveChangesAsync();

        var token = GenerateToken(user.Id);

        // Set HttpOnly cookie
        SetAuthCookie(token);

        return StatusCode(201, new AuthResponse(
            "注册成功",
            null,  // Token is in cookie, not body
            new UserDto(user.Id, user.Username, user.Email)
        ));
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { error = "请填写邮箱和密码" });
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null)
        {
            return BadRequest(new { error = "邮箱或密码错误" });
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.Password))
        {
            return BadRequest(new { error = "邮箱或密码错误" });
        }

        var token = GenerateToken(user.Id);

        // Set HttpOnly cookie
        SetAuthCookie(token);

        return Ok(new AuthResponse(
            "登录成功",
            null,  // Token is in cookie, not body
            new UserDto(user.Id, user.Username, user.Email)
        ));
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        // Clear the auth cookie
        Response.Cookies.Delete("auth_token");
        return Ok(new { message = "注销成功" });
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserResponse>> GetCurrentUser()
    {
        var userId = GetUserId();
        if (userId == null)
        {
            return Unauthorized(new { error = "请先登录" });
        }

        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return Unauthorized(new { error = "用户不存在" });
        }

        return Ok(new UserResponse(new UserDto(user.Id, user.Username, user.Email)));
    }

    private string GenerateToken(long userId)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT_SECRET not configured")));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddDays(AppConstants.TokenExpirationDays),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private void SetAuthCookie(string token)
    {
        // 判断是否为 HTTPS 或本地开发环境
        var isSecure = Request.Scheme == "https" || Request.Host.Host == "localhost" || Request.Host.Host == "127.0.0.1";
        Response.Cookies.Append("auth_token", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = isSecure,
            SameSite = isSecure ? SameSiteMode.Strict : SameSiteMode.Lax,
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddDays(AppConstants.TokenExpirationDays)
        });
    }
}
