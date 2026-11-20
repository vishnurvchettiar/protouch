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
    private readonly string[] _legacyRoots;
    private readonly ILogger<DietPlanDocumentService> _logger;

    public DietPlanDocumentService(
        ApplicationDbContext db,
        IWebHostEnvironment environment,
        ILogger<DietPlanDocumentService> logger,
        AppStorage? storage = null)
    {
        _db = db;
        // Where documents were kept before they moved into the database (read only).
        _legacyRoots = new[]
        {
            storage != null ? Path.Combine(storage.Root, "diet-plan-documents") : "",
            Path.Combine(environment.ContentRootPath, "App_Data", "diet-plan-documents")
        }.Where(root => root.Length > 0).ToArray();
        _logger = logger;
    }

    /// <summary>The client's current document without its bytes (the bytes can be large; read them with ReadContentAsync).</summary>
    public Task<DietPlanDocument?> GetCurrentAsync(string clientUserId) =>
        _db.DietPlanDocuments.AsNoTracking()
            .Where(document => document.ClientUserId == clientUserId)
            .Select(document => new DietPlanDocument
            {
                Id = document.Id,
                ClientUserId = document.ClientUserId,
                StoredFileName = document.StoredFileName,
                OriginalFileName = document.OriginalFileName,
                UploadedAtUtc = document.UploadedAtUtc
            })
            .FirstOrDefaultAsync();

    public async Task<(DietPlanDocument? Document, string? ErrorMessage)> SaveAsync(
        string clientUserId,
        IBrowserFile file)
    {
        var extension = Path.GetExtension(file.Name).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            return (null, "Choose a PDF, DOC, or DOCX file.");
        if (file.Size <= 0 || file.Size > MaxFileSizeBytes)
            return (null, "The document must be between 1 byte and 15 MB.");

        try
        {
            byte[] bytes;
            await using (var input = file.OpenReadStream(MaxFileSizeBytes))
            await using (var memory = new MemoryStream())
            {
                await input.CopyToAsync(memory);
                bytes = memory.ToArray();
            }

            if (!HasExpectedSignature(bytes, extension))
                return (null, "The selected file does not match its PDF, DOC, or DOCX extension.");

            var storageName = $"{Guid.NewGuid():N}{extension}";
            var oldDocument = await _db.DietPlanDocuments
                .FirstOrDefaultAsync(document => document.ClientUserId == clientUserId);
            var document = new DietPlanDocument
            {
                ClientUserId = clientUserId,
                StoredFileName = storageName,
                OriginalFileName = SanitizeFileName(file.Name),
                UploadedAtUtc = DateTime.UtcNow,
                Content = bytes
            };

            if (oldDocument == null)
                _db.DietPlanDocuments.Add(document);
            else
            {
                document.Id = oldDocument.Id;
                _db.Entry(oldDocument).CurrentValues.SetValues(document);
            }

            await _db.SaveChangesAsync();

            // Return a copy without the bytes so the page does not keep a large file in memory.
            return (new DietPlanDocument
            {
                Id = document.Id,
                ClientUserId = document.ClientUserId,
                StoredFileName = document.StoredFileName,
                OriginalFileName = document.OriginalFileName,
                UploadedAtUtc = document.UploadedAtUtc
            }, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Could not save diet plan document for client {ClientUserId}", clientUserId);
            return (null, "The document could not be saved. Please try again.");
        }
    }

    /// <summary>The document's bytes: from the database, or from the old file on disk for rows saved before the move.</summary>
    public async Task<byte[]?> ReadContentAsync(DietPlanDocument document)
    {
        var content = await _db.DietPlanDocuments.AsNoTracking()
            .Where(item => item.Id == document.Id)
            .Select(item => item.Content)
            .FirstOrDefaultAsync();
        if (content != null) return content;

        var path = LegacyPath(document);
        return path == null ? null : await File.ReadAllBytesAsync(path);
    }

    /// <summary>Copies documents that still live only on disk into the database. Safe to run on every start.</summary>
    public async Task<int> BackfillFromDiskAsync()
    {
        var copied = 0;
        var pending = await _db.DietPlanDocuments
            .Where(document => document.Content == null)
            .ToListAsync();
        foreach (var document in pending)
        {
            var path = LegacyPath(document);
            if (path == null) continue;
            document.Content = await File.ReadAllBytesAsync(path);
            copied++;
        }
        if (copied > 0) await _db.SaveChangesAsync();
        return copied;
    }

    private string? LegacyPath(DietPlanDocument document)
    {
        if (!Guid.TryParseExact(Path.GetFileNameWithoutExtension(document.StoredFileName), "N", out _))
            return null;

        var extension = Path.GetExtension(document.StoredFileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            return null;

        foreach (var root in _legacyRoots)
        {
            var path = Path.Combine(root, DirectoryKey(document.ClientUserId), document.StoredFileName);
            if (File.Exists(path)) return path;
        }
        return null;
    }

    private static string DirectoryKey(string clientUserId) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(clientUserId)));

    private static string SanitizeFileName(string fileName)
    {
        var safeName = Path.GetFileName(fileName);
        var cleaned = new string(safeName.Where(character => !char.IsControl(character)).ToArray()).Trim();
        return cleaned.Length <= 180 ? cleaned : cleaned[^180..];
    }

    private static bool HasExpectedSignature(byte[] bytes, string extension)
    {
        ReadOnlySpan<byte> header = bytes.AsSpan(0, Math.Min(8, bytes.Length));
        var read = header.Length;
        return extension switch
        {
            ".pdf" => read >= 5 && header[..5].SequenceEqual("%PDF-"u8),
            ".doc" => read >= 8 && header.SequenceEqual(new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }),
            ".docx" => read >= 4 && header[..4].SequenceEqual(new byte[] { 0x50, 0x4B, 0x03, 0x04 }),
            _ => false
        };
    }
}
