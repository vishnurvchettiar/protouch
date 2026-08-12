using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace ClinicApp.Data;

public class ClientProfile
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey(nameof(UserId))]
    public ApplicationUser? User { get; set; }

    public double Height { get; set; } // in cm
    public double StartWeight { get; set; } // in kg
    public double TargetWeight { get; set; } // in kg
    public double WaistInches { get; set; }
    public double ChestInches { get; set; }
    public double HipsInches { get; set; }
    public double ArmsInches { get; set; }
    public string DoctorNotes { get; set; } = string.Empty;
    // Stored as a relative path in runtime settings to avoid a database migration for media files.
    [NotMapped]
    public string ProfilePhotoUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime JoiningDate { get; set; } = DateTime.UtcNow;
}

public class DailyLog
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey(nameof(UserId))]
    public ApplicationUser? User { get; set; }

    public DateTime LogDate { get; set; } = DateTime.Today;
    public double Weight { get; set; } // in kg
    public double WaistInches { get; set; }
    public double ChestInches { get; set; }
    public double HipsInches { get; set; }
    public double ArmsInches { get; set; }
    public string DietNotes { get; set; } = string.Empty;
    public string MealPhotoUrl { get; set; } = string.Empty; // Store base64 or path

    // Macro-nutrient ratios in percentages (Carbs + Protein + Fat + Veg = 100)
    public int CarbsPercentage { get; set; }
    public int ProteinPercentage { get; set; }
    public int FatPercentage { get; set; }
    public int VegPercentage { get; set; }

    public DateTime LoggedAt { get; set; } = DateTime.UtcNow;
}

public class AttendanceRecord
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey(nameof(UserId))]
    public ApplicationUser? User { get; set; }

    public DateTime CheckInTime { get; set; } = DateTime.UtcNow;
    public DateTime? CheckOutTime { get; set; }
    public string TreatmentReceived { get; set; } = string.Empty; // e.g., Slimming Therapy, Physio Session
    public string Therapist { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public DateTime? NextAppointmentDate { get; set; }
    public string Status { get; set; } = "Checked In"; // Checked In, Completed, Absent
}

public class Appointment
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey(nameof(UserId))]
    public ApplicationUser? User { get; set; }

    public DateTime AppointmentDate { get; set; } = DateTime.UtcNow.AddDays(1);
    public string TreatmentType { get; set; } = string.Empty; // Slimming, Physio, Checkup
    public string Status { get; set; } = "Scheduled"; // Scheduled, Completed, Cancelled
    public string Notes { get; set; } = string.Empty;
}

public class PaymentNotification
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey(nameof(UserId))]
    public ApplicationUser? User { get; set; }

    public decimal Amount { get; set; }
    public DateTime BillingDate { get; set; } = DateTime.UtcNow;
    public string Description { get; set; } = string.Empty;
    public bool IsPaid { get; set; } = false;
    public bool IsTriggered { get; set; } = false; // Admin sent WhatsApp/In-app reminder
    public DateTime? TriggeredAt { get; set; }
}

public class WhatsAppLog
{
    [Key]
    public int Id { get; set; }

    public string TargetAudience { get; set; } = string.Empty; // e.g. "All Clients", "Active Slimming"
    public string MessageTemplate { get; set; } = string.Empty;
    public int RecipientCount { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Sent"; // Sent, Failed
    public string LogDetail { get; set; } = string.Empty; // JSON or text list of recipients
}

// Phone OTP model for verification flows
public class PhoneOtp
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string PhoneNumber { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddMinutes(5);

    public bool IsUsed { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

// Simple runtime setting storage for admin toggles
public class Setting
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}
