using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ClinicApp.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<ClientProfile> ClientProfiles { get; set; }
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
}

