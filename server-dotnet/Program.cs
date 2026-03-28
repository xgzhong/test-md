using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using server_dotnet.Converters;
using server_dotnet.Constants;
using server_dotnet.Data;
using server_dotnet.Middleware;
using Yitter.IdGenerator;

// Configure Serilog early
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/app-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting application");

    var builder = WebApplication.CreateBuilder(args);

    // Use Serilog
    builder.Host.UseSerilog();

// Read connection string from environment variable with fallback
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    ?? (builder.Configuration.GetConnectionString("DefaultConnection") is { } configConnStr
        && !configConnStr.StartsWith("${")
        ? configConnStr
        : null);

if (string.IsNullOrWhiteSpace(connectionString) || connectionString.StartsWith("${"))
{
    throw new InvalidOperationException("Database connection string must be configured via DB_CONNECTION_STRING environment variable or appsettings.json");
}

// Read JWT secret from environment variable (must be at least 256 bits / 32 bytes for HS256)
var jwtSecretEnv = Environment.GetEnvironmentVariable("JWT_SECRET");
var jwtSecret = !string.IsNullOrWhiteSpace(jwtSecretEnv) ? jwtSecretEnv
    : (builder.Configuration["Jwt:Secret"] is { } jwtConfig && !jwtConfig.StartsWith("${") ? jwtConfig
    : null);

if (string.IsNullOrWhiteSpace(jwtSecret))
{
    throw new InvalidOperationException("JWT secret must be configured via JWT_SECRET environment variable or appsettings.json");
}

// Read CORS origins from environment variable
var corsOrigins = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS")
    ?? builder.Configuration["CORS:AllowedOrigins"]
    ?? "http://localhost:5173";

// Parse CORS origins
var allowedOrigins = corsOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries)
    .Select(o => o.Trim())
    .ToArray();

// Initialize Yitter IdGenerator (雪花ID生成器)
var snowflakeBaseTimeStr = builder.Configuration["SnowflakeId:BaseTime"] ?? "2026-03-15 00:00:00";
var snowflakeBaseTime = DateTime.ParseExact(snowflakeBaseTimeStr, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
var idGeneratorOptions = new IdGeneratorOptions(1)
{
    BaseTime = new DateTime(snowflakeBaseTime.Ticks, DateTimeKind.Utc)
};
YitIdHelper.SetIdGenerator(idGeneratorOptions);

// Configure JSON to serialize long values as strings and DateTime in yyyy-MM-dd HH:mm:ss format
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new LongToStringConverter());
        options.JsonSerializerOptions.Converters.Add(new NullableLongToStringConverter());
        options.JsonSerializerOptions.Converters.Add(new DateTimeConverter());
        options.JsonSerializerOptions.Converters.Add(new DateTimeOffsetConverter());
    });

// Configure OpenAPI/Swagger
builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure MySQL database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        connectionString,
        new MySqlServerVersion(new Version(5, 7, 0))));

// Configure JWT Authentication with cookie support
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
        // Support cookie-based authentication
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                // If token is in cookie, use it
                if (context.Request.Cookies.TryGetValue("auth_token", out var cookieToken))
                {
                    context.Token = cookieToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// Configure Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter("auth", _ =>
            new FixedWindowRateLimiterOptions
            {
                PermitLimit = AppConstants.AuthRateLimitPermitCount,
                Window = TimeSpan.FromMinutes(AppConstants.AuthRateLimitWindowMinutes)
            }));
});

// Configure CORS with restrictive policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("Restrictive", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .WithMethods("GET", "POST", "PUT", "DELETE")
              .WithHeaders("Content-Type", "Authorization")
              .AllowCredentials()
              .SetPreflightMaxAge(TimeSpan.FromHours(1));
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Markdown Notes API v1");
        options.RoutePrefix = "swagger";
    });
}

// Enable static file serving from wwwroot
app.UseDefaultFiles();
app.UseStaticFiles();

// 添加 ForwardedHeaders 以支持反向代理（nginx）传递 HTTPS
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

// Enable CORS
app.UseCors("Restrictive");

// Global exception handler
app.UseMiddleware<GlobalExceptionHandler>();

app.UseAuthentication();
app.UseAuthorization();

// Rate limiting
app.UseRateLimiter();

// Map routes
app.MapControllers();

// SPA Fallback - serve index.html for non-API routes
app.MapFallbackToFile("index.html");

// Root endpoint - serve index.html for SPA
app.MapGet("/", async (HttpContext context) =>
{
    context.Response.ContentType = "text/html";
    await context.Response.SendFileAsync("wwwroot/index.html");
});

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();
    Log.Information("Database initialized successfully");
}

Log.Information("Application started, listening on {Address}", app.Urls.FirstOrDefault());
app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
