using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using ClinicApp.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ClinicApp.Data;

/// <summary>One-time, additive SQLite-to-Atlas migration. The source DB is never modified.</summary>
public static class MongoDataMigrator
{
    public static async Task MigrateAsync(IServiceProvider services, string sqlitePath, string contentRoot)
    {
        if (!File.Exists(sqlitePath))
            throw new FileNotFoundException("SQLite source file was not found. The source database was not changed.", sqlitePath);

        await using var scope = services.CreateAsyncScope();
        var destination = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var mongo = scope.ServiceProvider.GetRequiredService<IMongoDatabase>();
        var fileStorage = scope.ServiceProvider.GetRequiredService<IFileStorage>();

        var collections = await mongo.ListCollectionNames().ToListAsync();
        foreach (var collectionName in collections)
        {
            var count = await mongo.GetCollection<BsonDocument>(collectionName)
                .CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
            if (count > 0)
                throw new InvalidOperationException($"Atlas database already contains documents in '{collectionName}'. Migration stopped without changing existing Atlas data. Use a new empty database for the initial import.");
        }

        var fullSourcePath = Path.GetFullPath(sqlitePath);
        var safeCopyPath = Path.Combine(Path.GetTempPath(), $"protouch-migration-{Guid.NewGuid():N}.db");
        try
        {
            await CreateConsistentSqliteCopyAsync(fullSourcePath, safeCopyPath);
            var sourceOptions = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite($"Data Source={safeCopyPath}")
                .Options;
            await using var source = new ApplicationDbContext(sourceOptions);

            // Apply the application's existing SQLite migrations to the temporary copy
            // so older local databases can be read using the current model.
            await source.Database.MigrateAsync();
            await destination.Database.EnsureCreatedAsync();

            var rowCount = 0;
            foreach (var entityType in source.Model.GetEntityTypes().OrderBy(entity => MigrationOrder(entity.ClrType)))
            {
                var method = typeof(MongoDataMigrator).GetMethod(nameof(CopyEntityTypeAsync), BindingFlags.NonPublic | BindingFlags.Static)!
                    .MakeGenericMethod(entityType.ClrType);
                var task = (Task<int>)method.Invoke(null, [source, destination, mongo])!;
                rowCount += await task;
            }

            var filesCopied = await MigrateReferencedFilesAsync(source, destination, fileStorage, contentRoot);
            await destination.SaveChangesAsync();
            Console.WriteLine($"MongoDB migration completed. Copied {rowCount} database records and {filesCopied} referenced uploads.");
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(safeCopyPath)) File.Delete(safeCopyPath);
            if (File.Exists(safeCopyPath + "-wal")) File.Delete(safeCopyPath + "-wal");
            if (File.Exists(safeCopyPath + "-shm")) File.Delete(safeCopyPath + "-shm");
        }
    }

    private static int MigrationOrder(Type type) => type == typeof(ApplicationUser) ? 0 :
        type == typeof(Microsoft.AspNetCore.Identity.IdentityRole) ? 1 : 2;

    private static async Task<int> CopyEntityTypeAsync<TEntity>(ApplicationDbContext source, ApplicationDbContext destination, IMongoDatabase mongo)
        where TEntity : class
    {
        var records = await source.Set<TEntity>().AsNoTracking().ToListAsync();
        if (records.Count == 0) return 0;
        destination.Set<TEntity>().AddRange(records);
        await destination.SaveChangesAsync();

        var idProperty = typeof(TEntity).GetProperty("Id");
        if (idProperty?.PropertyType == typeof(int))
        {
            var highestId = records.Max(record => (int)(idProperty.GetValue(record) ?? 0));
            var sequenceName = typeof(TEntity).FullName ?? typeof(TEntity).Name;
            await mongo.GetCollection<BsonDocument>("__protouch_sequences").UpdateOneAsync(
                Builders<BsonDocument>.Filter.Eq("_id", sequenceName),
                Builders<BsonDocument>.Update.Max("value", highestId),
                new UpdateOptions { IsUpsert = true });
        }
        return records.Count;
    }

    private static async Task CreateConsistentSqliteCopyAsync(string sourcePath, string copyPath)
    {
        await using var source = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = sourcePath,
            Mode = SqliteOpenMode.ReadOnly
        }.ToString());
        await using var copy = new SqliteConnection($"Data Source={copyPath}");
        await source.OpenAsync();
        await copy.OpenAsync();
        source.BackupDatabase(copy);
    }

    private static async Task<int> MigrateReferencedFilesAsync(
        ApplicationDbContext source,
        ApplicationDbContext destination,
        IFileStorage storage,
        string contentRoot)
    {
        var copied = 0;
        var logs = await source.DailyLogs.AsNoTracking().ToListAsync();
        foreach (var log in logs.Where(item => !string.IsNullOrWhiteSpace(item.MealPhotoUrl)))
        {
            var newUrl = await CopyImageAsync(log.MealPhotoUrl, log.UserId, contentRoot, storage);
            if (newUrl != null)
            {
                var target = await destination.DailyLogs.FindAsync(log.Id);
                if (target != null) target.MealPhotoUrl = newUrl;
                copied++;
            }
        }

        var settings = await source.Settings.AsNoTracking().Where(setting => setting.Key.StartsWith("ClientPhoto:")).ToListAsync();
        foreach (var setting in settings)
        {
            var owner = setting.Key["ClientPhoto:".Length..];
            var newUrl = await CopyImageAsync(setting.Value, owner, contentRoot, storage);
            if (newUrl != null)
            {
                var target = await destination.Settings.FindAsync(setting.Id);
                if (target != null) target.Value = newUrl;
                copied++;
            }
        }

        var documents = await source.DietPlanDocuments.AsNoTracking().ToListAsync();
        foreach (var document in documents)
        {
            var ownerKey = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(document.ClientUserId)));
            var path = Path.Combine(contentRoot, "App_Data", "diet-plan-documents", ownerKey, Path.GetFileName(document.StoredFileName));
            if (!File.Exists(path))
            {
                Console.Error.WriteLine($"Upload not found, so it could not be copied: diet plan document for client {document.ClientUserId}.");
                continue;
            }

            await using var stream = File.OpenRead(path);
            var storedId = await storage.SaveAsync(stream, document.OriginalFileName, document.ClientUserId, "diet-plan", "application/octet-stream");
            var target = await destination.DietPlanDocuments.FindAsync(document.Id);
            if (target != null) target.StoredFileName = storedId;
            copied++;
        }

        return copied;
    }

    private static async Task<string?> CopyImageAsync(string value, string ownerId, string contentRoot, IFileStorage storage)
    {
        Stream? stream = null;
        string contentType;
        string fileName;
        if (value.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
        {
            var comma = value.IndexOf(',');
            if (comma < 0) return null;
            var header = value[..comma];
            try { stream = new MemoryStream(Convert.FromBase64String(value[(comma + 1)..])); }
            catch (FormatException) { return null; }
            contentType = header["data:".Length..].Split(';')[0];
            fileName = "legacy-upload" + ExtensionFor(contentType);
        }
        else
        {
            if (!value.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase)) return null;
            var relative = Uri.UnescapeDataString(value.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
            var root = Path.GetFullPath(Path.Combine(contentRoot, "wwwroot")) + Path.DirectorySeparatorChar;
            var path = Path.GetFullPath(Path.Combine(contentRoot, relative));
            if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !File.Exists(path))
            {
                Console.Error.WriteLine($"Upload not found, so it could not be copied: {value}");
                return null;
            }
            stream = File.OpenRead(path);
            contentType = Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };
            fileName = Path.GetFileName(path);
        }

        await using (stream)
        {
            if (contentType is not ("image/jpeg" or "image/png" or "image/webp")) return null;
            var id = await storage.SaveAsync(stream, fileName, ownerId, "image", contentType);
            return $"/uploads/{Uri.EscapeDataString(ownerId)}/{id}";
        }
    }

    private static string ExtensionFor(string contentType) => contentType.ToLowerInvariant() switch
    {
        "image/jpeg" => ".jpg",
        "image/png" => ".png",
        "image/webp" => ".webp",
        _ => string.Empty
    };
}
