using ClinicApp.Data;
using ClinicApp.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Microsoft.Extensions.DependencyInjection;

namespace ClinicApp.Tests;

public class DatabaseProviderTests
{
    [Fact]
    public void RenderUrl_BecomesAnNpgsqlConnectionString()
    {
        var conn = DatabaseConnection.FromUrl("postgres://clinic_user:p%40ss%3Aword@dpg-abc123-a:5432/clinic_db");
        Assert.Contains("Host=dpg-abc123-a", conn);
        Assert.Contains("Database=clinic_db", conn);
        Assert.Contains("Username=clinic_user", conn);
        Assert.Contains("Password=p@ss:word", conn);
    }

    [Fact]
    public void UrlWithoutPort_UsesDefaultPort()
        => Assert.Contains("Port=5432", DatabaseConnection.FromUrl("postgresql://u:p@host/db"));

    [Fact]
    public void NoDatabaseUrl_MeansSqlite()
    {
        Assert.Null(DatabaseConnection.PostgresFromEnvironment(null, null));
        Assert.Null(DatabaseConnection.PostgresFromEnvironment("  ", ""));
    }

    [Fact]
    public void ConfiguredConnectionString_IsUsedWhenNoUrl()
        => Assert.Equal("Host=h;Database=d", DatabaseConnection.PostgresFromEnvironment(null, " Host=h;Database=d "));

    [Fact]
    public void DatabaseUrl_WinsOverConfiguredString()
        => Assert.Contains("Host=fromurl", DatabaseConnection.PostgresFromEnvironment("postgres://u:p@fromurl/db", "Host=other;Database=x"));

    // If the model changes and a migration is forgotten, the app refuses to start. These catch it in the build instead.
    [Fact]
    public void SqliteMigrations_MatchTheModel()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlite("Data Source=:memory:").Options;
        using var db = new ApplicationDbContext(options);
        Assert.Empty(PendingChanges(db));
    }

    [Fact]
    public void PostgresMigrations_MatchTheModel()
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);
        var options = new DbContextOptionsBuilder<PgApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=model-check;Username=x;Password=x").Options;
        using var db = new PgApplicationDbContext(options);
        Assert.Empty(PendingChanges(db));
    }

    private static IReadOnlyList<MigrationOperation> PendingChanges(DbContext db)
    {
        var snapshot = db.GetService<IMigrationsAssembly>().ModelSnapshot;
        Assert.NotNull(snapshot);
        var model = snapshot!.Model;
        if (model is IMutableModel mutable) model = mutable.FinalizeModel();
        model = db.GetService<IModelRuntimeInitializer>().Initialize(model);
        return db.GetService<IMigrationsModelDiffer>().GetDifferences(
            model.GetRelationalModel(),
            db.GetService<IDesignTimeModel>().Model.GetRelationalModel());
    }
}
