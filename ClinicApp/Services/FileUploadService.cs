using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace ClinicApp.Services;

/// <summary>
/// Hardened file upload helper. Validates extension, MIME type, and size.
/// Requirements: 6.4, 6.5, 6.6, 19.1, 19.2, 19.3
/// </summary>
public class FileUploadService
{
    private static readonly (string Extension, string MimeType)[] AllowList =
    [
        (".jpg",  "image/jpeg"),
        (".jpeg", "image/jpeg"),
        (".png",  "image/png"),
        (".webp", "image/webp"),
    ];

    private const long MaxFileSizeBytes = 8 * 1024 * 1024; // 8 MB

    private readonly IWebHostEnvironment _env;
    private readonly ILogger<FileUploadService> _logger;

    public FileUploadService(IWebHostEnvironment env, ILogger<FileUploadService> logger)
    {
        _env = env;
        _logger = logger;
    }

    /// <summary>
    /// Saves an uploaded image file after validating extension, content-type, and size.
    /// </summary>
    /// <param name="file">The browser file from InputFile component.</param>
    /// <param name="folder">Subfolder under wwwroot/uploads/.</param>
    /// <param name="prefix">File name prefix (e.g. userId).</param>
    /// <returns>(path, null) on success; (null, errorMessage) on rejection.</returns>
    public async Task<(string? Path, string? ErrorMessage)> SaveImageAsync(
        IBrowserFile file,
        string folder,
        string prefix)
    {
        var extension = Path.GetExtension(file.Name).ToLowerInvariant();
        var contentType = file.ContentType?.ToLowerInvariant() ?? string.Empty;

        // Validate extension AND declared content-type against allow list
        var allowed = false;
        foreach (var (allowedExt, allowedMime) in AllowList)
        {
            if (extension == allowedExt && contentType == allowedMime)
            {
                allowed = true;
                break;
            }
        }

        if (!allowed)
        {
            _logger.LogWarning("FileUploadService: rejected file '{Name}' with ext='{Ext}' mime='{Mime}'",
                file.Name, extension, contentType);
            return (null, "Only JPG, JPEG, PNG and WebP images are allowed.");
        }

        // Validate file size before writing any bytes
        if (file.Size > MaxFileSizeBytes)
        {
            _logger.LogWarning("FileUploadService: rejected file '{Name}' with size {Size} bytes (limit {Limit})",
                file.Name, file.Size, MaxFileSizeBytes);
            return (null, "File must be 8 MB or smaller.");
        }

        var directory = Path.Combine(_env.WebRootPath, "uploads", folder);
        Directory.CreateDirectory(directory);
        var fileName = $"{prefix}_{Guid.NewGuid():N}{extension}";
        var physicalPath = Path.Combine(directory, fileName);

        await using var source = file.OpenReadStream(MaxFileSizeBytes);
        await using var target = File.Create(physicalPath);
        await source.CopyToAsync(target);

        return ($"/uploads/{folder}/{fileName}", null);
    }

    /// <summary>
    /// Validates a file by name and size without writing to disk. Used in tests and previews.
    /// </summary>
    public static (bool IsValid, string? ErrorMessage) ValidateFile(string fileName, string contentType, long sizeBytes)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var mime = contentType?.ToLowerInvariant() ?? string.Empty;

        var allowed = false;
        foreach (var (allowedExt, allowedMime) in AllowList)
        {
            if (extension == allowedExt && mime == allowedMime)
            {
                allowed = true;
                break;
            }
        }

        if (!allowed)
            return (false, $"Extension '{extension}' with content-type '{mime}' is not allowed.");

        if (sizeBytes > MaxFileSizeBytes)
            return (false, $"File size {sizeBytes} bytes exceeds 8 MB limit.");

        return (true, null);
    }
}
