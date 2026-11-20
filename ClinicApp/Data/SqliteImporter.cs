using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;

namespace ClinicApp.Data;

/// <summary>
/// One-time copy of an existing SQLite database into an empty PostgreSQL database, so moving to PostgreSQL
/// loses nothing. It only runs when PostgreSQL has no users yet and has not been imported before.
/// The SQLite file is copied first and the copy is read: the original is never changed or deleted.
/// Everything is copied inside one transaction and the row counts are checked before it is committed;
/// if anything is off the transaction is rolled back and start-up fails, so a half-copied database is never used.
/// </summary>
public static class SqliteImporter
{
    public const string DoneKey = "Import:SqliteDone";

    public static async Task<ImportResult> ImportIfNeededAsync(ApplicationDbContext target, IEnumerable<string?> candidatePaths, ILogger logger)
    {
        if (await target.Settings.AnyAsync(s => s.Key == DoneKey))
            return ImportResult.Skipped("already imported");
        if (await target.Users.AnyAsync())
            return ImportResult.Skipped("PostgreSQL already has users");

        var source = candidatePaths
            .Where(p => !string.IsNullOrWhiteSpace(p) && File.Exists(p) && new FileInfo(p!).Length > 0)
            .Select(p => p!)
            .FirstOrDefault();
        if (source == null)
            return ImportResult.Skipped("no SQLite file found");

        logger.LogWarning("Importing existing data from {Source} into PostgreSQL. The original file is not changed.", source);

        // Work on a copy so the original (and any write-ahead log) stays exactly as it is.
        var temp = Path.Combine(Path.GetTempPath(), "ptx-import-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        var copy = Path.Combine(temp, "app.db");
        try
        {
            foreach (var suffix in new[] { "", "-wal", "-shm" })
                if (File.Exists(source + suffix)) File.Copy(source + suffix, copy + suffix, overwrite: true);

            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite($"Data Source={copy}").Options;
            await using var from = new ApplicationDbContext(options);
            from.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
            // An older file may be missing newer tables or columns. Bring the copy up to date first.
            await from.Database.MigrateAsync();

            return await CopyAsync(from, target, source, logger);
        }
        finally
        {
            try { Directory.Delete(temp, recursive: true); } catch { /* temporary files only */ }
        }
    }

    private static async Task<ImportResult> CopyAsync(ApplicationDbContext from, ApplicationDbContext to, string source, ILogger logger)
    {
        var types = Ordered(from.Model.GetEntityTypes()
            .Where(t => !t.IsOwned() && t.FindPrimaryKey() != null && t.ClrType != typeof(Microsoft.AspNetCore.DataProtection.EntityFrameworkCore.DataProtectionKey))
            .ToList());

        var counts = new Dictionary<string, int>();
        var strategy = to.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            to.ChangeTracker.Clear();
            counts.Clear();
            await using var tx = await to.Database.BeginTransactionAsync();

            foreach (var type in types)
            {
                var rows = await ReadAllAsync(from, type.ClrType);
                counts[type.GetTableName() ?? type.ClrType.Name] = rows.Count;
                for (var i = 0; i < rows.Count; i += 500)
                {
                    foreach (var row in rows.Skip(i).Take(500)) to.Add(row);
                    await to.SaveChangesAsync();
                    to.ChangeTracker.Clear();
                }
            }

            // Every table must have exactly as many rows as the source.
            foreach (var type in types)
            {
                var name = type.GetTableName() ?? type.ClrType.Name;
                var inTarget = await CountAsync(to, type.ClrType);
                if (inTarget != counts[name])
                    throw new InvalidOperationException($"Import check failed for {name}: {counts[name]} rows in SQLite but {inTarget} in PostgreSQL. Nothing was imported.");
            }

            await ResetSequencesAsync(to, types);
            to.Settings.Add(new Setting { Key = DoneKey, Value = $"{DateTime.UtcNow:O} from {Path.GetFileName(source)}: {counts.Values.Sum()} rows" });
            await to.SaveChangesAsync();
            await tx.CommitAsync();
        });

        var total = counts.Values.Sum();
        logger.LogWarning("Imported {Rows} rows from SQLite into PostgreSQL: {Detail}", total,
            string.Join(", ", counts.Where(c => c.Value > 0).Select(c => $"{c.Key}={c.Value}")));
        return new ImportResult(true, total, "imported");
    }

    /// <summary>Parents before children, so foreign keys are satisfied.</summary>
    private static List<IEntityType> Ordered(List<IEntityType> types)
    {
        var result = new List<IEntityType>();
        var remaining = new List<IEntityType>(types);
        while (remaining.Count > 0)
        {
            var next = remaining.FirstOrDefault(t => t.GetForeignKeys()
                .Select(fk => fk.PrincipalEntityType)
                .All(p => p == t || !remaining.Contains(p)));
            next ??= remaining[0]; // a cycle should not exist; avoid looping forever if it does
            result.Add(next);
            remaining.Remove(next);
        }
        return result;
    }

    private static readonly System.Reflection.MethodInfo SetMethod =
        typeof(DbContext).GetMethods().First(m => m.Name == nameof(DbContext.Set) && m.IsGenericMethodDefinition && m.GetParameters().Length == 0);

    private static async Task<List<object>> ReadAllAsync(ApplicationDbContext db, Type clr)
    {
        var set = SetMethod.MakeGenericMethod(clr).Invoke(db, null)!;
        var list = new List<object>();
        await foreach (var item in ((IAsyncEnumerableAccessor)Activator.CreateInstance(typeof(Accessor<>).MakeGenericType(clr), set)!).Items())
            list.Add(item);
        return list;
    }

    private static async Task<int> CountAsync(ApplicationDbContext db, Type clr)
    {
        var set = SetMethod.MakeGenericMethod(clr).Invoke(db, null)!;
        return await ((IAsyncEnumerableAccessor)Activator.CreateInstance(typeof(Accessor<>).MakeGenericType(clr), set)!).CountAsync();
    }

    private interface IAsyncEnumerableAccessor
    {
        IAsyncEnumerable<object> Items();
        Task<int> CountAsync();
    }

    private sealed class Accessor<T>(DbSet<T> set) : IAsyncEnumerableAccessor where T : class
    {
        public async IAsyncEnumerable<object> Items()
        {
            await foreach (var item in set.AsNoTracking().AsAsyncEnumerable()) yield return item;
        }

        public Task<int> CountAsync() => set.CountAsync();
    }

    /// <summary>After inserting rows with their original ids, make the next generated id continue after the highest one.</summary>
    private static async Task ResetSequencesAsync(ApplicationDbContext db, List<IEntityType> types)
    {
        foreach (var type in types)
        {
            var key = type.FindPrimaryKey()!;
            if (key.Properties.Count != 1) continue;
            var prop = key.Properties[0];
            if (prop.ClrType != typeof(int) && prop.ClrType != typeof(long)) continue;
            if (prop.ValueGenerated != ValueGenerated.OnAdd) continue;

            var table = type.GetTableName();
            var column = prop.GetColumnName();
#pragma warning disable EF1002 // identifiers come from the model, not from user input
            await db.Database.ExecuteSqlRawAsync(
                $"SELECT setval(pg_get_serial_sequence('\"{table}\"', '{column}'), COALESCE((SELECT MAX(\"{column}\") FROM \"{table}\"), 0) + 1, false)");
#pragma warning restore EF1002
        }
    }
}

public sealed record ImportResult(bool Imported, int Rows, string Reason)
{
    public static ImportResult Skipped(string reason) => new(false, 0, reason);
}
