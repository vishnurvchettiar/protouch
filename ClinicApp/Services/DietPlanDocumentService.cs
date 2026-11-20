using ClinicApp.Data;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.Services;

public sealed class DietPlanDocumentService
{
    public const long MaxFileSizeBytes = 15 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions = [".pdf", ".doc", ".docx"];
    private readonly ApplicationDbContext _db;
    private readonly IFileStorage _storage;
    private readonly ILogger<DietPlanDocumentService> _logger;

    public DietPlanDocumentService(
        ApplicationDbContext db,
        IFileStorage storage,
        ILogger<DietPlanDocumentService> logger)
    {
        _db = db;
        _storage = storage;
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

        var storageName = $"{Guid.NewGuid():N}{extension}";
        string? storedId = null;

        try
        {
            await using var input = new MemoryStream();
            await using (var source = file.OpenReadStream(MaxFileSizeBytes))
                await source.CopyToAsync(input);
            if (!HasExpectedSignature(input, extension))
                return (null, "The selected file does not match its PDF, DOC, or DOCX extension.");

            input.Position = 0;
            storedId = await _storage.SaveAsync(input, storageName, clientUserId, "diet-plan", "application/octet-stream");

            var oldDocument = await _db.DietPlanDocuments
                .FirstOrDefaultAsync(document => document.ClientUserId == clientUserId);
            var oldStoredFileName = oldDocument?.StoredFileName;
            var document = new DietPlanDocument
            {
                ClientUserId = clientUserId,
                StoredFileName = storedId,
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

            if (oldStoredFileName != null && !string.Equals(oldStoredFileName, storedId, StringComparison.Ordinal))
                await _storage.DeleteAsync(oldStoredFileName);

            return (document, null);
        }
        catch (Exception ex)
        {
            await _storage.DeleteAsync(storedId);
            _logger.LogError(ex, "Could not save diet plan document for client {ClientUserId}", clientUserId);
            return (null, "The document could not be saved. Please try again.");
        }
    }

    public Task<StoredMongoFile?> OpenReadAsync(DietPlanDocument document, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(document.OriginalFileName).ToLowerInvariant();
        return AllowedExtensions.Contains(extension)
            ? _storage.OpenReadAsync(document.StoredFileName, document.ClientUserId, cancellationToken)
            : Task.FromResult<StoredMongoFile?>(null);
    }

    private static string SanitizeFileName(string fileName)
    {
        var safeName = Path.GetFileName(fileName);
        var cleaned = new string(safeName.Where(character => !char.IsControl(character)).ToArray()).Trim();
        return cleaned.Length <= 180 ? cleaned : cleaned[^180..];
    }

    private static bool HasExpectedSignature(Stream stream, string extension)
    {
        Span<byte> header = stackalloc byte[8];
        stream.Position = 0;
        var read = stream.Read(header);
        return extension switch
        {
            ".pdf" => read >= 5 && header[..5].SequenceEqual("%PDF-"u8),
            ".doc" => read >= 8 && header.SequenceEqual(new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }),
            ".docx" => read >= 4 && header[..4].SequenceEqual(new byte[] { 0x50, 0x4B, 0x03, 0x04 }),
            _ => false
        };
    }

}
