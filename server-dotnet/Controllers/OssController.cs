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
    /// 转换图片 URL（用于 Vditor 粘贴 markdown 图片时自动上传到 OSS）
    /// POST /api/oss/link-to-img
    /// </summary>
    [HttpPost("link-to-img")]
    public async Task<IActionResult> LinkToImg([FromBody] LinkToImgRequest linkRequest)
    {
        var url = linkRequest.Url;
        if (string.IsNullOrEmpty(url))
        {
            return BadRequest(new { error = "缺少 URL 参数" });
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

        try
        {
            // 下载原图
            using var client = new HttpClient();
            var imageBytes = await client.GetByteArrayAsync(url);

            // 从 URL 推断文件类型
            var uri = new Uri(url);
            var fileName = Path.GetFileName(uri.LocalPath);
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext)) ext = ".png";

            // 生成安全的文件名
            var newFileName = GenerateSafeFileName(fileName, ext);
            var objectName = $"uploads/temp/{newFileName}";

            // 清理 endpoint
            var cleanEndpoint = endpoint.Replace("https://", "").Replace("http://", "").TrimEnd('/');

            // 上传到 OSS
            var gmtDate = DateTime.UtcNow.ToString("ddd, dd MMM yyyy HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture) + " GMT";
            var mimeType = GetMimeType(ext);
            var resourcePath = $"/{bucketName}/{objectName}";

            var stringToSign = $"PUT\n\n{mimeType}\n{gmtDate}\n{resourcePath}";

            using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(accessKeySecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign));
            var signature = Convert.ToBase64String(hash);

            var ossUrl = $"https://{bucketName}.{cleanEndpoint}/{objectName}";

            var request = WebRequest.Create(ossUrl) as HttpWebRequest;
            request.Method = "PUT";
            request.ContentLength = imageBytes.Length;
            request.ContentType = mimeType;
            request.Date = DateTime.UtcNow;
            request.Host = $"{bucketName}.{cleanEndpoint}";
            request.Headers.Add("Authorization", $"OSS {accessKeyId}:{signature}");

            using (var requestStream = await request.GetRequestStreamAsync())
            {
                await requestStream.WriteAsync(imageBytes, 0, imageBytes.Length);
            }

            var response = await request.GetResponseAsync() as HttpWebResponse;
            var statusCode = (int)response.StatusCode;

            if (statusCode == 200)
            {
                return Ok(new { dest = ossUrl });
            }

            return BadRequest(new { error = $"上传失败 ({statusCode})" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting image URL: {Url}", url);
            return BadRequest(new { error = $"转换失败: {ex.Message}" });
        }
    }

    private static string GetMimeType(string extension)
    {
        return extension.ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".bmp" => "image/bmp",
            ".svg" => "image/svg+xml",
            _ => "application/octet-stream"
        };
    }

    /// <summary>
    /// 生成安全的文件名：原文件名（排除特殊字符）+ 随机字符串 + 扩展名
    /// </summary>
    private static string GenerateSafeFileName(string originalFileName, string ext)
    {
        // 移除非法的文件名字符，只保留字母、数字、中文、下划线、连字符、点
        var safeName = new StringBuilder();
        foreach (var c in Path.GetFileNameWithoutExtension(originalFileName))
        {
            if (char.IsLetterOrDigit(c) || char.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.OtherLetter
                || c == '_' || c == '-' || c == '.')
            {
                safeName.Append(c);
            }
        }
        // 移除开头和结尾的点（防止隐藏文件）
        var name = safeName.ToString().Trim('.');
        if (string.IsNullOrEmpty(name)) name = "file";
        // 限制文件名长度
        if (name.Length > 100) name = name[..100];

        var random = Random.Shared.Next(1000, 9999);
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd");
        return $"{timestamp}_{name}_{random}{ext}";
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
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg", ".bmp", ".tif", ".tiff", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".zip", ".rar", ".txt", ".md", ".sql", ".bak", ".cs", ".js", ".vue", ".html", ".htm", ".css", ".sass" };
        var ext = Path.GetExtension(request.FileName).ToLowerInvariant();

        // 如果没有扩展名或扩展名不在列表中，尝试从 ContentType 推断
        if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
        {
            var contentType = request.ContentType?.ToLowerInvariant() ?? "";
            ext = contentType switch
            {
                // 图片
                "image/png" => ".png",
                "image/jpeg" or "image/jpg" => ".jpg",
                "image/gif" => ".gif",
                "image/webp" => ".webp",
                "image/bmp" => ".bmp",
                "image/svg+xml" => ".svg",
                "image/tiff" => ".tiff",
                // PDF
                "application/pdf" => ".pdf",
                // Office 文档 - Word
                "application/msword" => ".doc",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ".docx",
                // Office 文档 - Excel
                "application/vnd.ms-excel" => ".xls",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => ".xlsx",
                // Office 文档 - PowerPoint
                "application/vnd.ms-powerpoint" => ".ppt",
                "application/vnd.openxmlformats-officedocument.presentationml.presentation" => ".pptx",
                // 压缩文件
                "application/zip" => ".zip",
                "application/x-zip-compressed" => ".zip",
                "application/x-rar-compressed" => ".rar",
                "application/vnd.rar" => ".rar",
                // 文本文件
                "text/plain" => ".txt",
                "text/markdown" => ".md",
                "text/x-sql" or "application/sql" => ".sql",
                "text/css" => ".css",
                "text/html" => ".html",
                "application/javascript" or "text/javascript" => ".js",
                // 代码文件
                "text/x-csharp" => ".cs",
                "text/x-vue" or "text/html" => ".vue",
                _ => ext
            };

            if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
            {
                return BadRequest(new { error = "不支持的文件类型" });
            }
        }

        // 验证文件大小
        if (request.FileSize > 200 * 1024 * 1024)
        {
            return BadRequest(new { error = "文件大小超过限制（最大 200MB）" });
        }

        // 生成安全的文件名
        var newFileName = GenerateSafeFileName(request.FileName, ext);

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

        if (file.Length > 200 * 1024 * 1024)
        {
            return BadRequest(new { error = "文件大小超过限制（最大 200MB）" });
        }

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        var newFileName = GenerateSafeFileName(file.FileName, ext);

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

public class LinkToImgRequest
{
    public string Url { get; set; } = string.Empty;
}

public class PresignedUrlRequest
{
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? ContentType { get; set; }
    public string? NoteId { get; set; }
}
