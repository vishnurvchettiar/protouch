using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ClinicApp.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IDataProtectionKeyContext
{
    private readonly IMongoDatabase? _mongoDatabase;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IMongoDatabase? mongoDatabase = null) : base(options)
    {
        _mongoDatabase = mongoDatabase;
    }

    /// <summary>Used by provider-specific contexts (see PgApplicationDbContext).</summary>
    protected ApplicationDbContext(DbContextOptions options) : base(options) { }

    public DbSet<ClientProfile> ClientProfiles { get; set; }
    public DbSet<DietPlanDocument> DietPlanDocuments { get; set; }
    public DbSet<DailyLog> DailyLogs { get; set; }
    public DbSet<AttendanceRecord> AttendanceRecords { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<PaymentNotification> PaymentNotifications { get; set; }
    public DbSet<WhatsAppLog> WhatsAppLogs { get; set; }

    // Phone OTPs for verification
    public DbSet<PhoneOtp> PhoneOtps { get; set; }

    // Simple runtime settings
    public DbSet<Setting> Settings { get; set; }

    // Public booking requests from the /book page
    public DbSet<BookingRequest> BookingRequests { get; set; }

    // Patient feedback from the homepage feedback widget
    public DbSet<SiteFeedback> SiteFeedbacks { get; set; }

    // Uploaded photos, kept in the database so they survive redeploys
    public DbSet<StoredFile> StoredFiles { get; set; }

    // Keys that keep people signed in, kept in the database so a deploy does not log everyone out
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<DietPlanDocument>()
            .HasIndex(document => document.ClientUserId)
            .IsUnique();

        builder.Entity<DietPlanDocument>()
            .HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(document => document.ClientUserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<StoredFile>()
            .HasIndex(file => file.Path)
            .IsUnique();
        builder.Entity<ApplicationUser>().HasIndex(user => user.NormalizedUserName).IsUnique();
        builder.Entity<IdentityRole>().HasIndex(role => role.NormalizedName).IsUnique();

        if (Database.ProviderName?.Contains("Mongo", StringComparison.OrdinalIgnoreCase) == true)
        {
            foreach (var entity in builder.Model.GetEntityTypes())
            {
                var id = entity.FindProperty("Id");
                if (id?.ClrType == typeof(int))
                    builder.Entity(entity.ClrType).Property("Id").ValueGeneratedNever();
            }
        }
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        AssignMongoIdsAsync(CancellationToken.None).GetAwaiter().GetResult();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        SaveChangesAsync(true, cancellationToken);

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        await AssignMongoIdsAsync(cancellationToken);
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private async Task AssignMongoIdsAsync(CancellationToken cancellationToken)
    {
        if (_mongoDatabase == null || Database.ProviderName?.Contains("Mongo", StringComparison.OrdinalIgnoreCase) != true) return;
        var added = ChangeTracker.Entries().Where(entry => entry.State == EntityState.Added)
            .Select(entry => new { Entry = entry, Id = entry.Metadata.FindProperty("Id") })
            .Where(item => item.Id?.ClrType == typeof(int) && Equals(item.Entry.Property("Id").CurrentValue, 0)).ToList();
        if (added.Count == 0) return;
        var counters = _mongoDatabase.GetCollection<BsonDocument>("__protouch_sequences");
        foreach (var item in added)
        {
            var name = item.Entry.Metadata.ClrType.FullName ?? item.Entry.Metadata.ClrType.Name;
            var result = await counters.FindOneAndUpdateAsync(Builders<BsonDocument>.Filter.Eq("_id", name),
                Builders<BsonDocument>.Update.Inc("value", 1), new FindOneAndUpdateOptions<BsonDocument>
                { IsUpsert = true, ReturnDocument = ReturnDocument.After }, cancellationToken);
            item.Entry.Property("Id").CurrentValue = result["value"].ToInt32();
        }
    }
}

/// <summary>
/// The same model on PostgreSQL. It exists only so PostgreSQL has its own migration history:
/// SQLite migrations stay on ApplicationDbContext, PostgreSQL migrations live in Data/MigrationsPg.
/// At runtime it is registered as ApplicationDbContext, so the rest of the app is unaware of it.
/// </summary>
public class PgApplicationDbContext : ApplicationDbContext
{
    public PgApplicationDbContext(DbContextOptions<PgApplicationDbContext> options) : base(options) { }
}
