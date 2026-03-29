using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace server_dotnet.Controllers;

/// <summary>
/// 阿里云 OSS 直传控制器
/// 客户端直接上传文件到 OSS，不经过服务器
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OssController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<OssController> _logger;

    public OssController(IConfiguration configuration, ILogger<OssController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// 获取上传文件的预签名 URL
    /// 客户端直接使用此 URL 上传文件到 OSS
    /// </summary>
    [HttpPost("presigned-url")]
    public IActionResult GetPresignedUrl([FromBody] PresignedUrlRequest request)
    {
        var accessKeyId = _configuration["OSS:AccessKeyId"];
        var accessKeySecret = _configuration["OSS:AccessKeySecret"];
        var endpoint = _configuration["OSS:Endpoint"];
        var bucketName = _configuration["OSS:BucketName"];
        var cdnDomain = _configuration["OSS:CdnDomain"];

        if (string.IsNullOrEmpty(accessKeyId) || string.IsNullOrEmpty(accessKeySecret) ||
            string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(bucketName))
        {
            return BadRequest(new { error = "OSS 未配置" });
        }

        // 验证文件类型
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg", ".bmp", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".zip", ".rar" };
        var ext = Path.GetExtension(request.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(ext))
        {
            return BadRequest(new { error = "不支持的文件类型" });
        }

        // 验证文件大小（默认 10MB）
        var maxSize = 10 * 1024 * 1024;
        if (request.FileSize > maxSize)
        {
            return BadRequest(new { error = "文件大小超过限制（最大 10MB）" });
        }

        // 生成唯一文件名
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var random = Random.Shared.Next(1000, 9999);
        var newFileName = $"{timestamp}_{random}{ext}";

        // 按笔记ID存储，未保存的笔记使用 temp 前缀
        var folderPrefix = string.IsNullOrEmpty(request.NoteId) || request.NoteId == "new"
            ? "temp"
            : request.NoteId;
        var objectName = $"uploads/{folderPrefix}/{newFileName}";

        // 生成预签名 URL（有效期 30 分钟）
        var expiration = DateTimeOffset.UtcNow.AddMinutes(30);
        var expirationStr = expiration.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

        // 构建签名
        var canonicalString = BuildCanonicalString("PUT", objectName, expirationStr, accessKeyId, bucketName);
        var signature = ComputeSignature(accessKeySecret, canonicalString);

        // 构建上传 URL（使用 VPC 内网 endpoint 可省流量费）
        var uploadUrl = $"{endpoint.TrimEnd('/')}/{objectName}";

        // 如果配置了 CDN 域名，最终 URL 使用 CDN
        var finalUrl = !string.IsNullOrEmpty(cdnDomain)
            ? $"https://{cdnDomain.Replace("https://", "").Replace("http://", "").TrimEnd('/')}/{objectName}"
            : $"https://{bucketName}.{endpoint.Replace("https://", "").Replace("http://", "").TrimEnd('/')}/{objectName}";

        // 返回预签名 URL 和其他参数
        // Vditor 需要这个格式来上传
        return Ok(new
        {
            uploadUrl,
            objectName,
            finalUrl,
            accessKeyId,
            signature,
            expiration = expirationStr,
            bucketName,
            policy = Convert.ToBase64String(Encoding.UTF8.GetBytes("{}"))
        });
    }

    private string BuildCanonicalString(string method, string objectName, string expiration, string accessKeyId, string bucketName)
    {
        var sb = new StringBuilder();
        sb.Append(method);
        sb.Append('\n');
        sb.Append('\n');
        sb.Append('\n');
        sb.Append(expiration);
        sb.Append('\n');
        sb.Append($"/{bucketName}/{objectName}");
        return sb.ToString();
    }

    private string ComputeSignature(string secret, string canonicalString)
    {
        using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(canonicalString));
        return Convert.ToBase64String(hash);
    }
}

public class PresignedUrlRequest
{
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? ContentType { get; set; }
    public string? NoteId { get; set; }
}
