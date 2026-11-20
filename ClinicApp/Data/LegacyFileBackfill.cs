using ClinicApp.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ClinicApp.Data;

/// <summary>
/// Copies photos that were saved as files (before they moved into the database) into the StoredFiles table.
/// Only files that are not in the database yet are copied, so it is safe to run on every start. Files are never deleted.
/// </summary>
public static class LegacyFileBackfill
{
    private static readonly Dictionary<string, string> Types = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg", [".png"] = "image/png", [".webp"] = "image/webp"
    };

    public static async Task<int> RunAsync(ApplicationDbContext db, IWebHostEnvironment env, AppStorage? storage, ILogger logger)
    {
        var roots = new List<string>();
        if (storage != null) roots.Add(Path.Combine(storage.Root, "uploads"));
        if (!string.IsNullOrEmpty(env.WebRootPath)) roots.Add(Path.Combine(env.WebRootPath, "uploads"));

        var known = new HashSet<string>(await db.StoredFiles.Select(f => f.Path).ToListAsync());
        var copied = 0;
        foreach (var root in roots.Distinct().Where(Directory.Exists))
        {
            foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            {
                if (!Types.TryGetValue(Path.GetExtension(file), out var contentType)) continue;
                var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
                if (relative.Count(c => c == '/') != 1) continue; // expects <folder>/<file>
                var publicPath = "uploads/" + relative;
                if (!known.Add(publicPath)) continue;

                db.StoredFiles.Add(new StoredFile
                {
                    Path = publicPath,
                    ContentType = contentType,
                    Data = await File.ReadAllBytesAsync(file),
                    CreatedAtUtc = File.GetCreationTimeUtc(file)
                });
                if (++copied % 50 == 0) await db.SaveChangesAsync();
            }
        }
        if (copied > 0)
        {
            await db.SaveChangesAsync();
            logger.LogWarning("Copied {Count} photo file(s) from disk into the database.", copied);
        }
        return copied;
    }
}
