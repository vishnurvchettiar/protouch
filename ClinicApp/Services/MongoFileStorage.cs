using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace ClinicApp.Services;

public interface IFileStorage
{
    Task<string> SaveAsync(Stream content, string fileName, string ownerId, string category, string contentType, CancellationToken cancellationToken = default);
    Task<StoredMongoFile?> OpenReadAsync(string id, string ownerId, CancellationToken cancellationToken = default);
    Task DeleteAsync(string? id, CancellationToken cancellationToken = default);
    Task DeleteOwnerAsync(string ownerId, CancellationToken cancellationToken = default);
}

/// <summary>Durable user-upload storage backed by the MongoDB database's GridFS bucket.</summary>
public sealed class MongoFileStorage(GridFSBucket bucket) : IFileStorage
{
    public async Task<string> SaveAsync(
        Stream content,
        string fileName,
        string ownerId,
        string category,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var options = new GridFSUploadOptions
        {
            Metadata = new BsonDocument
            {
                ["ownerId"] = ownerId,
                ["category"] = category,
                ["contentType"] = contentType
            }
        };
        var id = await bucket.UploadFromStreamAsync(fileName, content, options, cancellationToken);
        return id.ToString();
    }

    public async Task<StoredMongoFile?> OpenReadAsync(string id, string ownerId, CancellationToken cancellationToken = default)
    {
        if (!ObjectId.TryParse(id, out var objectId))
            return null;

        using var cursor = await bucket.FindAsync(
            Builders<GridFSFileInfo>.Filter.Eq(file => file.Id, objectId),
            cancellationToken: cancellationToken);
        var info = await cursor.FirstOrDefaultAsync(cancellationToken);
        if (info == null || info.Metadata == null ||
            !info.Metadata.TryGetValue("ownerId", out var storedOwner) ||
            storedOwner.AsString != ownerId)
            return null;

        var stream = await bucket.OpenDownloadStreamAsync(objectId, cancellationToken: cancellationToken);
        var contentType = info.Metadata.TryGetValue("contentType", out var type)
            ? type.AsString
            : "application/octet-stream";
        return new StoredMongoFile(stream, contentType, info.Filename);
    }

    public async Task DeleteAsync(string? id, CancellationToken cancellationToken = default)
    {
        if (ObjectId.TryParse(id, out var objectId))
            await bucket.DeleteAsync(objectId, cancellationToken);
    }

    public async Task DeleteOwnerAsync(string ownerId, CancellationToken cancellationToken = default)
    {
        using var cursor = await bucket.FindAsync(
            Builders<GridFSFileInfo>.Filter.Eq("metadata.ownerId", ownerId),
            cancellationToken: cancellationToken);
        var files = await cursor.ToListAsync(cancellationToken);
        foreach (var file in files)
            await bucket.DeleteAsync(file.Id, cancellationToken);
    }
}

public sealed record StoredMongoFile(Stream Content, string ContentType, string FileName) : IAsyncDisposable
{
    public ValueTask DisposeAsync() => Content.DisposeAsync();
}

/// <summary>Local development fallback. Production is required to configure MongoDB.</summary>
public sealed class LocalFileStorage(IWebHostEnvironment environment) : IFileStorage
{
    private readonly string _root = Path.Combine(environment.ContentRootPath, "App_Data", "local-uploads");

    public async Task<string> SaveAsync(Stream content, string fileName, string ownerId, string category, string contentType, CancellationToken cancellationToken = default)
    {
        var id = Guid.NewGuid().ToString("N");
        var owner = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(ownerId)));
        var directory = Path.Combine(_root, owner);
        Directory.CreateDirectory(directory);
        await using var output = new FileStream(Path.Combine(directory, id), FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await content.CopyToAsync(output, cancellationToken);
        await File.WriteAllTextAsync(Path.Combine(directory, id + ".meta"), $"{contentType}\n{Path.GetFileName(fileName)}", cancellationToken);
        return id;
    }

    public async Task<StoredMongoFile?> OpenReadAsync(string id, string ownerId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParseExact(id, "N", out _)) return null;
        var owner = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(ownerId)));
        var directory = Path.Combine(_root, owner);
        var path = Path.Combine(directory, id);
        var metadata = Path.Combine(directory, id + ".meta");
        if (!File.Exists(path) || !File.Exists(metadata)) return null;
        var values = await File.ReadAllLinesAsync(metadata, cancellationToken);
        return new StoredMongoFile(File.OpenRead(path), values.ElementAtOrDefault(0) ?? "application/octet-stream", values.ElementAtOrDefault(1) ?? id);
    }

    public Task DeleteAsync(string? id, CancellationToken cancellationToken = default)
    {
        if (Guid.TryParseExact(id, "N", out _))
        {
            if (Directory.Exists(_root))
            {
                foreach (var metadata in Directory.EnumerateFiles(_root, id + ".meta", SearchOption.AllDirectories))
                {
                    var path = Path.Combine(Path.GetDirectoryName(metadata)!, id);
                    File.Delete(path);
                    File.Delete(metadata);
                }
            }
        }
        return Task.CompletedTask;
    }

    public Task DeleteOwnerAsync(string ownerId, CancellationToken cancellationToken = default)
    {
        var owner = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(ownerId)));
        var directory = Path.Combine(_root, owner);
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        return Task.CompletedTask;
    }
}
