using System.Security.Cryptography;
using System.Text;
using ClinicApp.Data;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.Services;

public sealed class DietPlanDocumentService
{
    public const long MaxFileSizeBytes = 15 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions = [".pdf", ".doc", ".docx"];
    private readonly ApplicationDbContext _db;
    private readonly string _storageRoot;
    private readonly ILogger<DietPlanDocumentService> _logger;

    public DietPlanDocumentService(
        ApplicationDbContext db,
        IWebHostEnvironment environment,
        ILogger<DietPlanDocumentService> logger)
    {
        _db = db;
        _storageRoot = Path.Combine(environment.ContentRootPath, "App_Data", "diet-plan-documents");
        _logger = logger;
    }

    public Task<DietPlanDocument?> GetCurrentAsync(string clientUserId) =>
        _db.DietPlanDocuments.AsNoTracking()
            .FirstOrDefaultAsync(document => document.ClientUserId == clientUserId);

    public async Task<(DietPlanDocument? Document, string? ErrorMessage)> SaveAsync(
        string clientUserId,
        IBrowserFile file)
    {
        var extension = Path.GetExtension(file.Name).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            return (null, "Choose a PDF, DOC, or DOCX file.");
        if (file.Size <= 0 || file.Size > MaxFileSizeBytes)
            return (null, "The document must be between 1 byte and 15 MB.");

        var clientDirectory = GetClientDirectory(clientUserId);
        Directory.CreateDirectory(clientDirectory);
        var storageName = $"{Guid.NewGuid():N}{extension}";
        var physicalPath = Path.Combine(clientDirectory, storageName);

        try
        {
            await using (var input = file.OpenReadStream(MaxFileSizeBytes))
            await using (var output = new FileStream(physicalPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await input.CopyToAsync(output);
            }

            if (!HasExpectedSignature(physicalPath, extension))
            {
                File.Delete(physicalPath);
                return (null, "The selected file does not match its PDF, DOC, or DOCX extension.");
            }

            var oldDocument = await _db.DietPlanDocuments
                .FirstOrDefaultAsync(document => document.ClientUserId == clientUserId);
            var oldStoredFileName = oldDocument?.StoredFileName;
            var document = new DietPlanDocument
            {
                ClientUserId = clientUserId,
                StoredFileName = storageName,
                OriginalFileName = SanitizeFileName(file.Name),
                UploadedAtUtc = DateTime.UtcNow
            };

            if (oldDocument == null)
                _db.DietPlanDocuments.Add(document);
            else
            {
                document.Id = oldDocument.Id;
                _db.Entry(oldDocument).CurrentValues.SetValues(document);
            }

            await _db.SaveChangesAsync();

            if (oldStoredFileName != null && !string.Equals(oldStoredFileName, storageName, StringComparison.Ordinal))
                TryDeleteStoredFile(clientUserId, oldStoredFileName);

            return (document, null);
        }
        catch (Exception ex)
        {
            TryDeleteStoredFile(clientUserId, storageName);
            _logger.LogError(ex, "Could not save diet plan document for client {ClientUserId}", clientUserId);
            return (null, "The document could not be saved. Please try again.");
        }
    }

    public string? GetDownloadPath(DietPlanDocument document)
    {
        if (!Guid.TryParseExact(Path.GetFileNameWithoutExtension(document.StoredFileName), "N", out _))
            return null;

        var extension = Path.GetExtension(document.StoredFileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            return null;

        var path = Path.Combine(GetClientDirectory(document.ClientUserId), document.StoredFileName);
        return File.Exists(path) ? path : null;
    }

    private string GetClientDirectory(string clientUserId)
    {
        var directoryKey = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(clientUserId)));
        return Path.Combine(_storageRoot, directoryKey);
    }

    private static string SanitizeFileName(string fileName)
    {
        var safeName = Path.GetFileName(fileName);
        var cleaned = new string(safeName.Where(character => !char.IsControl(character)).ToArray()).Trim();
        return cleaned.Length <= 180 ? cleaned : cleaned[^180..];
    }

    private static bool HasExpectedSignature(string path, string extension)
    {
        Span<byte> header = stackalloc byte[8];
        using var stream = File.OpenRead(path);
        var read = stream.Read(header);
        return extension switch
        {
            ".pdf" => read >= 5 && header[..5].SequenceEqual("%PDF-"u8),
            ".doc" => read >= 8 && header.SequenceEqual(new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }),
            ".docx" => read >= 4 && header[..4].SequenceEqual(new byte[] { 0x50, 0x4B, 0x03, 0x04 }),
            _ => false
        };
    }

    private void TryDeleteStoredFile(string clientUserId, string storedFileName)
    {
        try
        {
            if (Path.GetFileName(storedFileName) == storedFileName)
                File.Delete(Path.Combine(GetClientDirectory(clientUserId), storedFileName));
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Could not remove replaced diet plan document for client {ClientUserId}", clientUserId);
        }
    }
}
