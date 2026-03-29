using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace server_dotnet.Controllers;

/// <summary>
/// 阿里云 OSS 上传控制器
/// 支持预签名 URL 直传和服务器中转上传
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
    /// 获取预签名 URL（用于浏览器直传 OSS）
    /// </summary>
    [HttpPost("presigned-url")]
    public IActionResult GetPresignedUrl([FromBody] PresignedUrlRequest request)
    {
        var accessKeyId = _configuration["OSS:AccessKeyId"];
        var accessKeySecret = _configuration["OSS:AccessKeySecret"];
        var endpoint = _configuration["OSS:Endpoint"];
        var bucketName = _configuration["OSS:BucketName"];

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

        // 验证文件大小
        if (request.FileSize > 10 * 1024 * 1024)
        {
            return BadRequest(new { error = "文件大小超过限制（最大 10MB）" });
        }

        // 生成文件名
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var random = Random.Shared.Next(1000, 9999);
        var newFileName = $"{timestamp}_{random}{ext}";

        // 按笔记ID存储
        var folderPrefix = string.IsNullOrEmpty(request.NoteId) || request.NoteId == "new"
            ? "temp"
            : request.NoteId;
        var objectName = $"uploads/{folderPrefix}/{newFileName}";

        try
        {
            // 清理 endpoint
            var cleanEndpoint = endpoint.Replace("https://", "").Replace("http://", "").TrimEnd('/');

            // 过期时间 30 分钟
            var expiration = DateTimeOffset.UtcNow.AddMinutes(30);
            var expires = ((DateTimeOffset)expiration).ToUnixTimeSeconds();

            // 资源路径
            var resourcePath = $"/{bucketName}/{objectName}";

            // Content-Type 用于 PUT 签名
            var contentType = request.ContentType ?? "application/octet-stream";

            // 签名字符串 (PUT 格式)
            var stringToSign = $"PUT\n\n{contentType}\n{expires}\n{resourcePath}";

            // 计算签名
            using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(accessKeySecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign));
            var signature = Convert.ToBase64String(hash);

            // 构建预签名 URL
            var presignedUrl = $"https://{bucketName}.{cleanEndpoint}/{objectName}?OSSAccessKeyId={accessKeyId}&Expires={expires}&Signature={Uri.EscapeDataString(signature)}";

            _logger.LogInformation("Generated presigned URL for {ObjectName}", objectName);

            return Ok(new
            {
                uploadUrl = presignedUrl,
                objectKey = objectName,
                finalUrl = $"https://{bucketName}.{cleanEndpoint}/{objectName}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating presigned URL");
            return BadRequest(new { error = $"生成上传链接失败: {ex.Message}" });
        }
    }

    /// <summary>
    /// 服务器中转上传（备用方案）
    /// </summary>
    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(IFormFile file, [FromQuery] string? noteId)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "请选择文件" });
        }

        var accessKeyId = _configuration["OSS:AccessKeyId"];
        var accessKeySecret = _configuration["OSS:AccessKeySecret"];
        var endpoint = _configuration["OSS:Endpoint"];
        var bucketName = _configuration["OSS:BucketName"];

        if (string.IsNullOrEmpty(accessKeyId) || string.IsNullOrEmpty(accessKeySecret) ||
            string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(bucketName))
        {
            return BadRequest(new { error = "OSS 未配置" });
        }

        if (file.Length > 10 * 1024 * 1024)
        {
            return BadRequest(new { error = "文件大小超过限制（最大 10MB）" });
        }

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var random = Random.Shared.Next(1000, 9999);
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var newFileName = $"{timestamp}_{random}{ext}";

        var folderPrefix = string.IsNullOrEmpty(noteId) || noteId == "new"
            ? "temp"
            : noteId;
        var objectName = $"uploads/{folderPrefix}/{newFileName}";

        try
        {
            var cleanEndpoint = endpoint.Replace("https://", "").Replace("http://", "").TrimEnd('/');

            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();

            var gmtDate = DateTime.UtcNow.ToString("ddd, dd MMM yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture) + " GMT";
            var contentType = file.ContentType ?? "application/octet-stream";
            var resourcePath = $"/{bucketName}/{objectName}";

            var stringToSign = $"PUT\n\n{contentType}\n{gmtDate}\n{resourcePath}";

            using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(accessKeySecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign));
            var signature = Convert.ToBase64String(hash);

            var url = $"https://{bucketName}.{cleanEndpoint}/{objectName}";

            var request = WebRequest.Create(url) as HttpWebRequest;
            request.Method = "PUT";
            request.ContentLength = fileBytes.Length;
            request.ContentType = contentType;
            request.Date = DateTime.UtcNow;
            request.Host = $"{bucketName}.{cleanEndpoint}";
            request.Headers.Add("Authorization", $"OSS {accessKeyId}:{signature}");

            using (var requestStream = await request.GetRequestStreamAsync())
            {
                await requestStream.WriteAsync(fileBytes, 0, fileBytes.Length);
            }

            var response = await request.GetResponseAsync() as HttpWebResponse;
            var statusCode = (int)response.StatusCode;

            _logger.LogInformation("Upload success: {StatusCode}", statusCode);

            var finalUrl = $"https://{bucketName}.{cleanEndpoint}/{objectName}";
            return Ok(new { url = finalUrl, objectKey = objectName });
        }
        catch (WebException ex)
        {
            var httpResponse = ex.Response as HttpWebResponse;
            var statusCode = httpResponse?.StatusCode ?? HttpStatusCode.BadRequest;
            var errorBody = "";
            try
            {
                using var reader = new StreamReader(ex.Response.GetResponseStream());
                errorBody = await reader.ReadToEndAsync();
            }
            catch { }

            _logger.LogError("OSS error: {StatusCode}, Body: {ErrorBody}", statusCode, errorBody);
            return BadRequest(new { error = $"上传失败 ({statusCode})" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upload error");
            return BadRequest(new { error = $"上传失败: {ex.Message}" });
        }
    }
}

public class PresignedUrlRequest
{
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? ContentType { get; set; }
    public string? NoteId { get; set; }
}
