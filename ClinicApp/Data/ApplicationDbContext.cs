using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IDataProtectionKeyContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

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
