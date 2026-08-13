using System;
using ClinicApp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using ClinicApp.Services;

namespace ClinicApp.Tests;

/// <summary>
/// Factory for creating isolated in-memory SQLite databases for property tests.
/// </summary>
public static class TestDbFactory
{
    public static ApplicationDbContext CreateInMemoryDb(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;
        var db = new ApplicationDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    public static ClinicService CreateClinicService(ApplicationDbContext db)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection([
                new("WhatsApp:Mode", "Mock"),
                new("WhatsApp:ClinicNumber", "9999999999")
            ])
            .Build();
        var logger = NullLogger<ClinicService>.Instance;
        return new ClinicService(db, config, logger);
    }
}
