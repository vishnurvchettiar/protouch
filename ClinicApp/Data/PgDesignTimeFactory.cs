using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ClinicApp.Data;

/// <summary>Lets "dotnet ef migrations add ... --context PgApplicationDbContext" work without a running database.</summary>
public class PgDesignTimeFactory : IDesignTimeDbContextFactory<PgApplicationDbContext>
{
    static PgDesignTimeFactory()
    {
        // Must match Program.cs, otherwise the migration is generated with different column types than the app uses.
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);
    }

    public PgApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<PgApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=design;Username=design;Password=design",
                o => o.MigrationsHistoryTable("__EFMigrationsHistory"))
            .Options;
        return new PgApplicationDbContext(options);
    }
}

/// <summary>Same for the SQLite migrations ("--context ApplicationDbContext").</summary>
public class SqliteDesignTimeFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite("Data Source=design-time.db")
            .Options;
        return new ApplicationDbContext(options);
    }
}
