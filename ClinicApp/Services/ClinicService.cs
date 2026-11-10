using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ClinicApp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace ClinicApp.Services;

public record CsvRecipient(string Name, string Phone);
public record CsvPreviewRow(string RecipientName, string RecipientPhone, string PersonalisedMessage, string? Warning = null);

/// <summary>Outcome of a bulk WhatsApp campaign.</summary>
public record CampaignResult(int Sent, int Simulated, int Failed, int Skipped, string Mode);

public class ClinicService
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<ClinicService> _logger;

    // ==========================================
    // Task 2.1: IST timezone helper (cross-platform)
    // ==========================================
    private static readonly TimeZoneInfo IST = GetIst();
    private static TimeZoneInfo GetIst()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"); }
        catch { return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"); }
    }

    public ClinicService(ApplicationDbContext db, IConfiguration config, ILogger<ClinicService> logger)
    {
        _db = db;
        _config = config;
        _logger = logger;
    }

    // New: read runtime setting from DB; fallback to configuration
    public string GetWhatsAppMode()
    {
        try
        {
            var s = _db.Settings.FirstOrDefault(st => st.Key == "WhatsApp:Mode");
            if (s != null && !string.IsNullOrEmpty(s.Value)) return s.Value;
        }
        catch { /* ignore if migrations not applied yet */ }
        return _config.GetValue<string>("WhatsApp:Mode") ?? "Mock";
    }

    public async Task SetSettingAsync(string key, string value)
    {
        var setting = await _db.Settings.FirstOrDefaultAsync(s => s.Key == key);
        if (setting == null)
        {
            setting = new Setting { Key = key, Value = value };
            _db.Settings.Add(setting);
        }
        else
        {
            setting.Value = value;
            _db.Settings.Update(setting);
        }
        await _db.SaveChangesAsync();
    }

    public async Task<string?> GetSettingAsync(string key)
    {
        var s = await _db.Settings.FirstOrDefaultAsync(st => st.Key == key);
        return s?.Value;
    }

    private (string Sid, string Token, string From) GetTwilioConfig()
    {
        // Prefer DB settings
        var sid = _db.Settings.FirstOrDefault(s => s.Key == "WhatsApp:Twilio:AccountSid")?.Value
                  ?? _config["WhatsApp:Twilio:AccountSid"];
        var token = _db.Settings.FirstOrDefault(s => s.Key == "WhatsApp:Twilio:AuthToken")?.Value
                    ?? _config["WhatsApp:Twilio:AuthToken"];
        var from = _db.Settings.FirstOrDefault(s => s.Key == "WhatsApp:Twilio:From")?.Value
                   ?? _config["WhatsApp:Twilio:From"] ?? "whatsapp:+14155238886";
        return (sid ?? string.Empty, token ?? string.Empty, from);
    }

    // ==========================================
    // Client Profiles Operations
    // ==========================================
    public async Task<List<ClientProfile>> GetAllClientsAsync()
    {
        var profiles = await _db.ClientProfiles
            .Include(cp => cp.User)
            .OrderBy(cp => cp.User != null ? cp.User.FullName : "")
            .ToListAsync();

        var userIds = profiles.Select(p => p.UserId).ToList();
        var photoKeys = userIds.Select(id => $"ClientPhoto:{id}").ToList();
        var photoSettings = await _db.Settings
            .Where(s => photoKeys.Contains(s.Key))
            .ToDictionaryAsync(s => s.Key, s => s.Value);

        foreach (var profile in profiles)
        {
            var key = $"ClientPhoto:{profile.UserId}";
            profile.ProfilePhotoUrl = photoSettings.TryGetValue(key, out var url) ? url : string.Empty;
        }
        return profiles;
    }

    public async Task<ClientProfile?> GetClientProfileByUserIdAsync(string userId)
    {
        var profile = await _db.ClientProfiles
            .Include(cp => cp.User)
            .FirstOrDefaultAsync(cp => cp.UserId == userId);
        if (profile != null) profile.ProfilePhotoUrl = await GetClientPhotoAsync(userId) ?? string.Empty;
        return profile;
    }

    public async Task UpdateClientProfileAsync(ClientProfile profile)
    {
        var existing = await _db.ClientProfiles.FindAsync(profile.Id);
        if (existing != null)
        {
            existing.Height = profile.Height;
            existing.StartWeight = profile.StartWeight;
            existing.TargetWeight = profile.TargetWeight;
            existing.WaistInches = profile.WaistInches;
            existing.ChestInches = profile.ChestInches;
            existing.HipsInches = profile.HipsInches;
            existing.ArmsInches = profile.ArmsInches;
            existing.DoctorNotes = profile.DoctorNotes;
            existing.IsActive = profile.IsActive;
            await _db.SaveChangesAsync();
        }
    }

    // ==========================================
    // UpdateClientDietPlanAsync — Requirement 11.1, 11.2, 11.3
    // ==========================================
    /// <summary>
    /// Updates the DietPlan field on a client's profile.
    /// Throws InvalidOperationException if the profile does not exist.
    /// </summary>
    public async Task UpdateClientDietPlanAsync(string userId, string dietPlan)
    {
        var profile = await _db.ClientProfiles
            .FirstOrDefaultAsync(cp => cp.UserId == userId);

        if (profile == null)
            throw new InvalidOperationException(
                $"Client profile not found for userId: {userId}");

        profile.DietPlan = dietPlan;
        await _db.SaveChangesAsync();
    }

    public async Task EnsureClientProfileExistsAsync(string userId, double startWeight = 70.0, double targetWeight = 65.0, double height = 170.0)
    {
        var exists = await _db.ClientProfiles.AnyAsync(cp => cp.UserId == userId);
        if (!exists)
        {
            try
            {
                var profile = new ClientProfile
                {
                    UserId = userId,
                    StartWeight = startWeight,
                    TargetWeight = targetWeight,
                    Height = height,
                    JoiningDate = DateTime.UtcNow,
                    IsActive = true
                };
                _db.ClientProfiles.Add(profile);
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Profile was created by a concurrent request — safe to ignore
            }
        }
    }

    public async Task<string?> GetClientPhotoAsync(string userId)
    {
        return await GetSettingAsync($"ClientPhoto:{userId}");
    }

    public async Task SetClientPhotoAsync(string userId, string relativePath)
    {
        await SetSettingAsync($"ClientPhoto:{userId}", relativePath);
    }

    // ==========================================
    // Task 4.4: Phone normalisation (Requirement 18)
    // ==========================================

    /// <summary>
    /// Normalise an Indian phone number to E.164 10-digit format (no country code),
    /// then prepend 91 for whatsapp.me links. Returns empty string on failure.
    /// </summary>
    public static string NormalisePhone(string? phone)
    {
        // Only extract ASCII digits (0-9), not Unicode lookalike digit chars
        var digits = new string((phone ?? string.Empty).Where(c => c >= '0' && c <= '9').ToArray());

        // Strip leading 00
        if (digits.StartsWith("00"))
            digits = digits[2..];

        // Strip leading 91 if result would be > 10 digits
        if (digits.Length > 10 && digits.StartsWith("91"))
            digits = digits[2..];

        // Must be exactly 10 digits now
        if (digits.Length != 10)
            return string.Empty;

        return "91" + digits;
    }

    /// <summary>
    /// Builds the address Twilio needs for WhatsApp ("whatsapp:+91XXXXXXXXXX").
    /// Returns null when the number is not a valid 10-digit Indian mobile number.
    /// Addresses that already start with "whatsapp:" are passed through unchanged.
    /// </summary>
    public static string? ToWhatsAppAddress(string? raw)
    {
        var trimmed = (raw ?? string.Empty).Trim();
        if (trimmed.StartsWith("whatsapp:", StringComparison.OrdinalIgnoreCase))
            return trimmed.Length > "whatsapp:".Length ? "whatsapp:" + trimmed["whatsapp:".Length..].Trim() : null;

        var normalised = NormalisePhone(trimmed);
        return string.IsNullOrEmpty(normalised) ? null : "whatsapp:+" + normalised;
    }

    public string BuildWhatsAppLink(string phone, string message)
    {
        var normalized = NormalisePhone(phone);
        if (string.IsNullOrEmpty(normalized))
        {
            _logger.LogError("BuildWhatsAppLink: could not normalise phone number '{Phone}'", phone);
            return string.Empty;
        }
        return $"https://wa.me/{normalized}?text={Uri.EscapeDataString(message ?? string.Empty)}";
    }

    public string BuildClinicWhatsAppLink(string message) =>
        BuildWhatsAppLink(_config["WhatsApp:ClinicNumber"] ?? string.Empty, message);

    public async Task<List<ApplicationUser>> GetUsersWithPhoneAsync()
    {
        return await _db.Users.Where(u => u.PhoneNumber != null && u.PhoneNumber != "")
            .OrderBy(u => u.FullName).ToListAsync();
    }

    // ==========================================
    // Daily Logs Operations
    // ==========================================
    public async Task<List<DailyLog>> GetClientDailyLogsAsync(string userId)
    {
        return await _db.DailyLogs
            .Where(dl => dl.UserId == userId)
            .OrderByDescending(dl => dl.LogDate)
            .ToListAsync();
    }

    public async Task AddDailyLogAsync(DailyLog log)
    {
        _db.DailyLogs.Add(log);
        await _db.SaveChangesAsync();
    }

    // ==========================================
    // Task 2.5: UpdateDailyLogAsync — upsert with macro validation (Req 7.4, 7.5, 8.3, 8.4)
    // ==========================================
    public async Task UpdateDailyLogAsync(DailyLog log)
    {
        // Validate UserId is not null or empty
        if (string.IsNullOrEmpty(log.UserId))
            throw new ArgumentException("UserId must not be null or empty.", nameof(log));

        // Validate Weight is positive
        if (log.Weight <= 0)
            throw new ArgumentException("Weight must be greater than zero.", nameof(log));

        // Validate LogDate is not in the future (beyond today)
        if (log.LogDate.Date > DateTime.Today)
            throw new ArgumentException("LogDate cannot be in the future.", nameof(log));

        // Validate individual macro values are non-negative and ≤ 100
        if (log.CarbsPercentage < 0 || log.ProteinPercentage < 0 || log.FatPercentage < 0 || log.VegPercentage < 0)
            throw new InvalidOperationException("Macro percentages cannot be negative.");

        if (log.CarbsPercentage > 100 || log.ProteinPercentage > 100 || log.FatPercentage > 100 || log.VegPercentage > 100)
            throw new InvalidOperationException("Macro percentages cannot exceed 100.");

        // Validate macro sum = 100
        var macroSum = log.CarbsPercentage + log.ProteinPercentage + log.FatPercentage + log.VegPercentage;
        if (macroSum != 100)
            throw new InvalidOperationException($"Macro percentages must sum to 100. Got {macroSum}.");

        // Try find by UserId + LogDate (upsert semantics)
        var existing = await _db.DailyLogs
            .FirstOrDefaultAsync(d => d.UserId == log.UserId && d.LogDate == log.LogDate);

        if (existing != null)
        {
            existing.Weight = log.Weight;
            existing.WaistInches = log.WaistInches;
            existing.ChestInches = log.ChestInches;
            existing.HipsInches = log.HipsInches;
            existing.ArmsInches = log.ArmsInches;
            existing.DietNotes = log.DietNotes;
            existing.MealPhotoUrl = log.MealPhotoUrl;
            existing.CarbsPercentage = log.CarbsPercentage;
            existing.ProteinPercentage = log.ProteinPercentage;
            existing.FatPercentage = log.FatPercentage;
            existing.VegPercentage = log.VegPercentage;
            existing.LoggedAt = DateTime.UtcNow;
            // Copy the Id back so caller can detect update vs insert
            log.Id = existing.Id;
        }
        else
        {
            // If log has an Id > 0 it might be an existing record by PK
            if (log.Id > 0)
            {
                var byId = await _db.DailyLogs.FindAsync(log.Id);
                if (byId != null)
                {
                    byId.Weight = log.Weight;
                    byId.WaistInches = log.WaistInches;
                    byId.ChestInches = log.ChestInches;
                    byId.HipsInches = log.HipsInches;
                    byId.ArmsInches = log.ArmsInches;
                    byId.DietNotes = log.DietNotes;
                    byId.MealPhotoUrl = log.MealPhotoUrl;
                    byId.CarbsPercentage = log.CarbsPercentage;
                    byId.ProteinPercentage = log.ProteinPercentage;
                    byId.FatPercentage = log.FatPercentage;
                    byId.VegPercentage = log.VegPercentage;
                    byId.LoggedAt = DateTime.UtcNow;
                }
                else
                {
                    _db.DailyLogs.Add(log);
                }
            }
            else
            {
                _db.DailyLogs.Add(log);
            }
        }

        await _db.SaveChangesAsync();
    }

    // ==========================================
    // Task 2.3: GetAllMealLogsAsync (Req 5.2, 5.4, 5.5)
    // ==========================================
    public async Task<List<DailyLog>> GetAllMealLogsAsync(int page = 1, int pageSize = 20)
    {
        // Fetch all with photos - use client-side filter for compatibility with in-memory provider
        var allLogs = await _db.DailyLogs
            .OrderByDescending(dl => dl.LogDate)
            .ToListAsync();

        // Filter client-side for non-empty photo URLs
        var filtered = allLogs
            .Where(dl => !string.IsNullOrEmpty(dl.MealPhotoUrl))
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        // Populate User navigation properties
        foreach (var log in filtered)
        {
            if (log.User == null && !string.IsNullOrEmpty(log.UserId))
            {
                log.User = await _db.Users.FindAsync(log.UserId);
            }
        }

        return filtered;
    }

    // ==========================================
    // Task 2.4: DeleteDailyLogAsync (Req 5.3)
    // ==========================================
    public async Task DeleteDailyLogAsync(int logId)
    {
        var log = await _db.DailyLogs.FindAsync(logId);
        if (log == null)
        {
            _logger.LogError("DeleteDailyLogAsync: DailyLog with Id={LogId} not found.", logId);
            throw new KeyNotFoundException($"DailyLog with Id {logId} was not found.");
        }
        _db.DailyLogs.Remove(log);
        await _db.SaveChangesAsync();
    }

    // ==========================================
    // Attendance & Treatments Operations
    // ==========================================
    public async Task<List<AttendanceRecord>> GetClientAttendanceAsync(string userId)
    {
        return await _db.AttendanceRecords
            .Where(ar => ar.UserId == userId)
            .OrderByDescending(ar => ar.CheckInTime)
            .ToListAsync();
    }

    public async Task<List<AttendanceRecord>> GetAllAttendanceAsync()
    {
        return await _db.AttendanceRecords
            .Include(ar => ar.User)
            .OrderByDescending(ar => ar.CheckInTime)
            .ToListAsync();
    }

    public async Task LogCheckInAsync(string userId, string treatment, string therapist, string notes, DateTime? nextAppointmentDate = null, DateTime? visitDate = null)
    {
        var record = new AttendanceRecord
        {
            UserId = userId,
            CheckInTime = visitDate?.ToUniversalTime() ?? DateTime.UtcNow,
            TreatmentReceived = treatment,
            Therapist = therapist,
            Notes = notes,
            NextAppointmentDate = nextAppointmentDate,
            Status = "Checked In"
        };
        _db.AttendanceRecords.Add(record);
        await _db.SaveChangesAsync();
    }

    public async Task LogCheckOutAsync(int recordId, string notes)
    {
        var record = await _db.AttendanceRecords.FindAsync(recordId);
        if (record != null)
        {
            record.CheckOutTime = DateTime.UtcNow;
            record.Status = "Completed";
            if (!string.IsNullOrEmpty(notes))
            {
                record.Notes += " | Checkout notes: " + notes;
            }
            await _db.SaveChangesAsync();
        }
    }

    // ==========================================
    // Task 2.1: DeleteAttendanceRecordAsync (Req 3.7, 3.8)
    // ==========================================
    public async Task DeleteAttendanceRecordAsync(int recordId)
    {
        var record = await _db.AttendanceRecords.FindAsync(recordId);
        if (record == null)
        {
            _logger.LogError("DeleteAttendanceRecordAsync: AttendanceRecord with Id={RecordId} not found.", recordId);
            return; // no-op if not found per spec
        }
        _db.AttendanceRecords.Remove(record);
        await _db.SaveChangesAsync();
    }

    // ==========================================
    // Task 2.2: UpdateAttendanceRecordAsync (Req 3.1–3.5)
    // ==========================================
    public async Task UpdateAttendanceRecordAsync(AttendanceRecord record)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(record.Therapist))
            throw new ArgumentException("Therapist must not be empty.", nameof(record));

        if (!string.IsNullOrEmpty(record.Notes) && record.Notes.Length > 2000)
            throw new ArgumentException("Notes must be 2000 characters or fewer.", nameof(record));

        if (record.CheckOutTime.HasValue && record.CheckOutTime.Value < record.CheckInTime)
            throw new ArgumentException("Check-out time must not be before check-in time.", nameof(record));

        var existing = await _db.AttendanceRecords.FindAsync(record.Id);
        if (existing == null)
        {
            _logger.LogError("UpdateAttendanceRecordAsync: AttendanceRecord with Id={RecordId} not found.", record.Id);
            throw new KeyNotFoundException($"AttendanceRecord with Id {record.Id} was not found.");
        }

        existing.TreatmentReceived = record.TreatmentReceived;
        existing.Therapist = record.Therapist;
        existing.Notes = record.Notes;
        existing.NextAppointmentDate = record.NextAppointmentDate;
        existing.CheckOutTime = record.CheckOutTime;
        existing.Status = record.Status;
        await _db.SaveChangesAsync();
    }

    // ==========================================
    // Appointments Operations
    // ==========================================
    public async Task<List<Appointment>> GetClientAppointmentsAsync(string userId)
    {
        return await _db.Appointments
            .Where(ap => ap.UserId == userId)
            .OrderBy(ap => ap.AppointmentDate)
            .ToListAsync();
    }

    public async Task<List<Appointment>> GetAllAppointmentsAsync()
    {
        return await _db.Appointments
            .Include(ap => ap.User)
            .OrderByDescending(ap => ap.AppointmentDate)
            .ToListAsync();
    }

    public async Task BookAppointmentAsync(Appointment appointment)
    {
        _db.Appointments.Add(appointment);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAppointmentStatusAsync(int id, string status)
    {
        var appt = await _db.Appointments.FindAsync(id);
        if (appt != null)
        {
            appt.Status = status;
            await _db.SaveChangesAsync();
        }
    }

    // ==========================================
    // Task 2.2: BuildReminderMessage (Req 2.1–2.4)
    // ==========================================
    public static string BuildReminderMessage(string clientName, string treatmentType, DateTime appointmentDateUtc)
    {
        var ist = GetIst();
        var localTime = TimeZoneInfo.ConvertTimeFromUtc(appointmentDateUtc, ist);
        var name = string.IsNullOrWhiteSpace(clientName) ? "Valued Client" : clientName;
        var treatment = string.IsNullOrWhiteSpace(treatmentType) ? "your appointment" : treatmentType;
        return $"""
            Hello {name}, this is a reminder from ProTouch Physio - Slimming Clinic.

            Your appointment for {treatment} is scheduled for tomorrow at {localTime:dd MMM yyyy, hh:mm tt}.

            📍 Row H. No. 22, Kasliwal Bhagyoday, Beed By-pass, Ch. Sambhajinagar.
            📞 96735 30031

            Reply STOP to unsubscribe.
            """;
    }

    // ==========================================
    // Task 2.3: UpdateAppointmentReminderSentAsync (Req 1.4, 3.3)
    // ==========================================
    public async Task UpdateAppointmentReminderSentAsync(int appointmentId, bool sent)
    {
        var appt = await _db.Appointments.FindAsync(appointmentId);
        if (appt == null) return;
        appt.ReminderSent = sent;
        await _db.SaveChangesAsync();
    }

    // ==========================================
    // Task 2.4: GetUpcomingAppointmentsForReminderAsync (Req 1.2, 1.4)
    // ==========================================
    public async Task<List<Appointment>> GetUpcomingAppointmentsForReminderAsync()
    {
        var now = DateTime.UtcNow;
        var windowStart = now.AddHours(23);
        var windowEnd = now.AddHours(25);

        return await _db.Appointments
            .Include(a => a.User)
            .Where(a =>
                (a.Status == "Scheduled" || a.Status == "Confirmed") &&
                !a.ReminderSent &&
                a.AppointmentDate >= windowStart &&
                a.AppointmentDate <= windowEnd)
            .ToListAsync();
    }

    // ==========================================
    // Req 6.1 / 6.2: Log appointment reminder to WhatsAppLogs
    // ==========================================
    public async Task LogAppointmentReminderAsync(string phone, string message, bool isManual)
    {
        var audience = isManual
            ? "Appointment Reminder (Manual)"
            : "Appointment Reminder (Auto)";

        _db.WhatsAppLogs.Add(new WhatsAppLog
        {
            TargetAudience = audience,
            MessageTemplate = message,
            RecipientCount = 1,
            SentAt = DateTime.UtcNow,
            Status = "Sent",
            LogDetail = System.Text.Json.JsonSerializer.Serialize(
                new[] { new { Phone = phone, Status = "Sent" } })
        });
        await _db.SaveChangesAsync();
    }

    // ==========================================
    // Task 3.1: SubmitBookingRequestAsync (Req 14.1–14.9)
    // ==========================================
    private static readonly Regex IndianPhonePattern = new(@"^[6-9]\d{9}$", RegexOptions.Compiled);

    public async Task<int> SubmitBookingRequestAsync(BookingRequest request)
    {
        // Validate FullName (2–100 chars)
        if (string.IsNullOrWhiteSpace(request.FullName) || request.FullName.Trim().Length < 2 || request.FullName.Trim().Length > 100)
            throw new ArgumentException("Full name must be between 2 and 100 characters.", nameof(request));

        // Validate Indian phone number
        var phoneDigits = new string(request.PhoneNumber.Where(char.IsDigit).ToArray());
        if (!IndianPhonePattern.IsMatch(phoneDigits))
            throw new ArgumentException("Phone number must be a valid 10-digit Indian mobile number.", nameof(request));

        // Validate ServiceType non-empty
        if (string.IsNullOrWhiteSpace(request.ServiceType))
            throw new ArgumentException("Service type must be selected.", nameof(request));

        // Validate Notes length (max 1000 chars)
        if (request.Notes != null && request.Notes.Length > 1000)
            throw new ArgumentException("Notes must not exceed 1000 characters.", nameof(request));

        // Validate PreferredDate: 1 hour to 90 days in the future
        var now = DateTime.UtcNow;
        var minDate = now.AddHours(1);
        var maxDate = now.AddDays(90);
        if (request.PreferredDate < minDate || request.PreferredDate > maxDate)
            throw new ArgumentException("Preferred date must be between 1 hour and 90 days from now.", nameof(request));

        // Set defaults
        request.Status = "Pending";
        request.SubmittedAt = DateTime.UtcNow;

        _db.BookingRequests.Add(request);
        await _db.SaveChangesAsync();

        // Notify clinic via WhatsApp with retry logic (up to 3 attempts, 20s interval)
        var clinicNumber = _config["WhatsApp:ClinicNumber"] ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(clinicNumber))
        {
            var notificationMsg = $"📅 New Booking Request!\nName: {request.FullName}\nPhone: {request.PhoneNumber}\nService: {request.ServiceType}\nPreferred Date: {request.PreferredDate:dd MMM yyyy, hh:mm tt}\nNotes: {request.Notes}";
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    await SendWhatsAppMessageAsync(clinicNumber, notificationMsg);
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "WhatsApp notification attempt {Attempt} failed for booking {BookingId}.", attempt, request.Id);
                    if (attempt < 3)
                        await Task.Delay(TimeSpan.FromSeconds(20));
                }
            }
        }

        return request.Id;
    }

    // ==========================================
    // Task 3.2: GetAllBookingRequestsAsync (Req 16.1)
    // ==========================================
    public async Task<List<BookingRequest>> GetAllBookingRequestsAsync()
    {
        return await _db.BookingRequests
            .OrderByDescending(br => br.SubmittedAt)
            .ToListAsync();
    }

    // ==========================================
    // Task 3.3: UpdateBookingRequestStatusAsync (Req 16.2, 16.3)
    // ==========================================
    public async Task UpdateBookingRequestStatusAsync(int id, string newStatus)
    {
        var booking = await _db.BookingRequests.FindAsync(id);
        if (booking == null)
            throw new KeyNotFoundException($"BookingRequest with Id {id} was not found.");

        // Enforce valid transitions
        var validTransitions = new Dictionary<string, HashSet<string>>
        {
            ["Pending"] = new HashSet<string> { "Confirmed", "Cancelled" },
            ["Confirmed"] = new HashSet<string> { "Cancelled" },
            ["Cancelled"] = new HashSet<string>()
        };

        if (!validTransitions.TryGetValue(booking.Status, out var allowedNext) || !allowedNext.Contains(newStatus))
            throw new InvalidOperationException($"Transition from '{booking.Status}' to '{newStatus}' is not allowed.");

        booking.Status = newStatus;
        await _db.SaveChangesAsync();
    }

    // ==========================================
    // Billing & Payment Notifications Operations
    // ==========================================
    public async Task<List<PaymentNotification>> GetClientPaymentsAsync(string userId)
    {
        return await _db.PaymentNotifications
            .Where(pn => pn.UserId == userId)
            .OrderByDescending(pn => pn.BillingDate)
            .ToListAsync();
    }

    public async Task<List<PaymentNotification>> GetAllPaymentsAsync()
    {
        return await _db.PaymentNotifications
            .Include(pn => pn.User)
            .OrderByDescending(pn => pn.BillingDate)
            .ToListAsync();
    }

    public async Task CreateInvoiceAsync(string userId, decimal amount, string description)
    {
        var invoice = new PaymentNotification
        {
            UserId = userId,
            Amount = amount,
            Description = description,
            BillingDate = DateTime.UtcNow,
            IsPaid = false,
            IsTriggered = false
        };
        _db.PaymentNotifications.Add(invoice);
        await _db.SaveChangesAsync();
    }

    public async Task TriggerPaymentAlertAsync(int invoiceId)
    {
        var invoice = await _db.PaymentNotifications
            .Include(i => i.User)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        if (invoice != null)
        {
            invoice.IsTriggered = true;
            invoice.TriggeredAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();

            string phone = invoice.User?.PhoneNumber ?? "9999999999";
            string clientName = invoice.User?.FullName ?? "Client";
            string msg = $"Hello {clientName}, this is an automated reminder from Slimming & Physio Clinic. Your payment of {invoice.Amount:C} is due for: {invoice.Description}. Please settle as soon as possible.";

            var log = new WhatsAppLog
            {
                TargetAudience = $"Single Client Payment Alert ({clientName})",
                MessageTemplate = msg,
                RecipientCount = 1,
                SentAt = DateTime.UtcNow,
                Status = "Sent",
                LogDetail = JsonSerializer.Serialize(new[] { new { Name = clientName, Phone = phone, Status = "Delivered" } })
            };
            _db.WhatsAppLogs.Add(log);
            await _db.SaveChangesAsync();
        }
    }

    public async Task MarkInvoiceAsPaidAsync(int invoiceId)
    {
        var invoice = await _db.PaymentNotifications.FindAsync(invoiceId);
        if (invoice != null)
        {
            invoice.IsPaid = true;
            await _db.SaveChangesAsync();
        }
    }

    // ==========================================
    // Bulk WhatsApp Logs & Operations
    // ==========================================
    public async Task DeleteClientDataAsync(string userId)
    {
        var logs = _db.DailyLogs.Where(d => d.UserId == userId);
        _db.DailyLogs.RemoveRange(logs);

        var visits = _db.AttendanceRecords.Where(a => a.UserId == userId);
        _db.AttendanceRecords.RemoveRange(visits);

        var appts = _db.Appointments.Where(a => a.UserId == userId);
        _db.Appointments.RemoveRange(appts);

        var payments = _db.PaymentNotifications.Where(p => p.UserId == userId);
        _db.PaymentNotifications.RemoveRange(payments);

        var profiles = _db.ClientProfiles.Where(c => c.UserId == userId);
        _db.ClientProfiles.RemoveRange(profiles);

        var photoKey = $"ClientPhoto:{userId}";
        var photoSetting = await _db.Settings.FirstOrDefaultAsync(s => s.Key == photoKey);
        if (photoSetting != null) _db.Settings.Remove(photoSetting);

        await _db.SaveChangesAsync();
    }

    public async Task<List<WhatsAppLog>> GetWhatsAppLogsAsync()
    {
        return await _db.WhatsAppLogs
            .OrderByDescending(wl => wl.SentAt)
            .ToListAsync();
    }

    public async Task LogManualWhatsAppCampaignAsync(string audienceType, string templateText, List<ApplicationUser> recipients)
    {
        var details = recipients.Select(u => new
        {
            Name = u.FullName,
            Phone = u.PhoneNumber,
            Status = "Prepared for WhatsApp"
        }).ToList();

        _db.WhatsAppLogs.Add(new WhatsAppLog
        {
            TargetAudience = audienceType,
            MessageTemplate = templateText,
            RecipientCount = recipients.Count,
            SentAt = DateTime.UtcNow,
            Status = "Prepared",
            LogDetail = JsonSerializer.Serialize(details)
        });
        await _db.SaveChangesAsync();
    }

    // ==========================================
    // Helper: build opt-out footer
    // ==========================================
    private async Task<string> GetOptOutFooterAsync()
    {
        var footer = await GetSettingAsync("WhatsApp:OptOutFooter");
        return string.IsNullOrWhiteSpace(footer) ? "Reply STOP to unsubscribe." : footer;
    }

    private static string AppendOptOutFooter(string body, string footer, out bool truncated)
    {
        truncated = false;
        const int maxLength = 4096;
        var suffix = "\n\n" + footer;
        if (body.Length + suffix.Length > maxLength)
        {
            var allowedBodyLength = maxLength - suffix.Length;
            body = body[..Math.Max(0, allowedBodyLength)];
            truncated = true;
        }
        return body + suffix;
    }

    // ==========================================
    // Task 4.1: WhatsApp campaigns with opt-out footer (Req 9.1–9.4)
    // ==========================================

    // Task 4.2: CSV preview (Req 10.1–10.6)
    public async Task<List<CsvPreviewRow>> PreviewCsvCampaignAsync(string template, IEnumerable<CsvRecipient> recipients)
    {
        var footer = await GetOptOutFooterAsync();
        var rows = new List<CsvPreviewRow>();
        var seenPhones = new HashSet<string>();
        foreach (var recipient in recipients)
        {
            var name = string.IsNullOrWhiteSpace(recipient.Name) ? "{{Name}}" : recipient.Name;
            var personalised = template
                .Replace("{{Name}}", name)
                .Replace("{Name}", name);
            AppendOptOutFooter(personalised, footer, out _);
            var finalMessage = personalised + "\n\n" + footer;
            string? warning = null;
            var normalised = NormalisePhone(recipient.Phone);
            if (string.IsNullOrEmpty(normalised))
                warning = "Invalid phone number. This recipient will be skipped.";
            else if (!seenPhones.Add(normalised))
                warning = "Duplicate number. Only the first one is sent.";
            rows.Add(new CsvPreviewRow(recipient.Name, recipient.Phone, finalMessage, warning));
        }
        // No DB writes — pure preview
        return rows;
    }

    // Task 4.3: CSV campaign with delay + progress (Req 11.1–11.4)
    public async Task<CampaignResult> SendCsvWhatsAppCampaignAsync(
        string campaignName,
        string template,
        IEnumerable<CsvRecipient> recipients,
        IProgress<(int sent, int failed, int total)>? progress = null)
    {
        var footer = await GetOptOutFooterAsync();
        var mode = GetWhatsAppMode();

        // Read delay setting
        var delayMs = 1000;
        var delaySetting = await GetSettingAsync("WhatsApp:BulkDelayMs");
        if (int.TryParse(delaySetting, out var parsedDelay) && parsedDelay >= 100 && parsedDelay <= 60000)
            delayMs = parsedDelay;

        // Skip blank numbers and repeats so nobody is messaged twice.
        var seen = new HashSet<string>();
        var recipientList = new List<CsvRecipient>();
        var skipped = 0;
        var dispatchDetails = new List<object>();
        foreach (var r in recipients.Where(r => !string.IsNullOrWhiteSpace(r.Phone)))
        {
            var key = NormalisePhone(r.Phone);
            if (!string.IsNullOrEmpty(key) && !seen.Add(key))
            {
                skipped++;
                dispatchDetails.Add(new { Name = r.Name, Phone = r.Phone, Status = "Skipped", Error = "Duplicate number" });
                continue;
            }
            recipientList.Add(r);
        }

        var total = recipientList.Count;
        var sent = 0;
        var failed = 0;
        var simulated = 0;

        foreach (var recipient in recipientList)
        {
            var message = template
                .Replace("{{Name}}", string.IsNullOrWhiteSpace(recipient.Name) ? "there" : recipient.Name)
                .Replace("{Name}", string.IsNullOrWhiteSpace(recipient.Name) ? "there" : recipient.Name);
            var messageWithFooter = AppendOptOutFooter(message, footer, out _);

            try
            {
                if (mode == "Twilio" && ToWhatsAppAddress(recipient.Phone) == null)
                    throw new ArgumentException("Not a valid 10-digit phone number.");

                await SendWhatsAppMessageAsync(recipient.Phone, messageWithFooter);
                if (mode == "Twilio") sent++; else simulated++;
                dispatchDetails.Add(new { Name = recipient.Name, Phone = recipient.Phone, Status = mode == "Twilio" ? "Sent" : "Simulated" });
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogError(ex, "Failed to send WhatsApp to {Phone}", recipient.Phone);
                dispatchDetails.Add(new { Name = recipient.Name, Phone = recipient.Phone, Status = "Failed", Error = ex.Message });
            }

            progress?.Report((sent + simulated, failed, total));

            if (delayMs > 0)
                await Task.Delay(delayMs);
        }

        var status = failed > 0 ? $"Partial ({failed} failed)"
                   : mode == "Twilio" ? "Bulk campaign dispatched"
                   : "Bulk campaign simulated";
        _db.WhatsAppLogs.Add(new WhatsAppLog
        {
            TargetAudience = campaignName,
            MessageTemplate = template,
            RecipientCount = sent + simulated,
            SentAt = DateTime.UtcNow,
            Status = status,
            LogDetail = JsonSerializer.Serialize(dispatchDetails)
        });
        await _db.SaveChangesAsync();

        return new CampaignResult(sent, simulated, failed, skipped, mode);
    }

    // ==========================================
    // Phone OTP flows
    // ==========================================
    public async Task<string> GeneratePhoneOtpAsync(string phone)
    {
        var recent = await _db.PhoneOtps
            .Where(o => o.PhoneNumber == phone && !o.IsUsed)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        if (recent != null && (DateTime.UtcNow - recent.CreatedAt).TotalSeconds < 60)
        {
            return recent.Code;
        }

        var rnd = new Random();
        var code = rnd.Next(100000, 999999).ToString();

        var otp = new PhoneOtp
        {
            PhoneNumber = phone,
            Code = code,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };
        _db.PhoneOtps.Add(otp);
        await _db.SaveChangesAsync();

        var mode = GetWhatsAppMode();
        if (mode == "Twilio")
        {
            var cfg = GetTwilioConfig();
            if (!string.IsNullOrEmpty(cfg.Sid) && !string.IsNullOrEmpty(cfg.Token))
            {
                TwilioClient.Init(cfg.Sid, cfg.Token);
                var msg = MessageResource.Create(
                    body: $"Your ProTouch verification code is: {code}",
                    from: new Twilio.Types.PhoneNumber(cfg.From),
                    to: new Twilio.Types.PhoneNumber(phone)
                );
                _db.WhatsAppLogs.Add(new WhatsAppLog
                {
                    TargetAudience = "OTP (Twilio)",
                    MessageTemplate = $"OTP to {phone}: {code}",
                    RecipientCount = 1,
                    SentAt = DateTime.UtcNow,
                    Status = msg.Status.ToString(),
                    LogDetail = JsonSerializer.Serialize(new[] { new { Phone = phone, Sid = msg.Sid } })
                });
                await _db.SaveChangesAsync();
                return code;
            }
        }

        // Mock path
        var log = new WhatsAppLog
        {
            TargetAudience = "OTP Simulation",
            MessageTemplate = $"OTP to {phone}: {code}",
            RecipientCount = 1,
            SentAt = DateTime.UtcNow,
            Status = "Simulated",
            LogDetail = JsonSerializer.Serialize(new[] { new { Phone = phone, Code = code, Status = "Simulated" } })
        };
        _db.WhatsAppLogs.Add(log);
        await _db.SaveChangesAsync();

        return code;
    }

    public async Task<bool> VerifyPhoneOtpAsync(string phone, string code)
    {
        var otp = await _db.PhoneOtps
            .Where(o => o.PhoneNumber == phone && !o.IsUsed && o.ExpiresAt >= DateTime.UtcNow)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        if (otp == null) return false;
        if (otp.Code != code) return false;

        otp.IsUsed = true;
        await _db.SaveChangesAsync();
        return true;
    }

    // ==========================================
    // WhatsApp sending wrapper (bulk) — Task 4.1 footer added
    // ==========================================
    public async Task SendWhatsAppMessageAsync(string toPhone, string messageText)
    {
        var mode = GetWhatsAppMode();
        if (mode == "Twilio")
        {
            var cfg = GetTwilioConfig();
            if (!string.IsNullOrEmpty(cfg.Sid) && !string.IsNullOrEmpty(cfg.Token))
            {
                var toAddress = ToWhatsAppAddress(toPhone)
                    ?? throw new ArgumentException($"'{toPhone}' is not a valid phone number.", nameof(toPhone));
                var fromAddress = cfg.From.StartsWith("whatsapp:", StringComparison.OrdinalIgnoreCase) ? cfg.From : "whatsapp:" + cfg.From;

                TwilioClient.Init(cfg.Sid, cfg.Token);
                var msg = await MessageResource.CreateAsync(
                    body: messageText,
                    from: new Twilio.Types.PhoneNumber(fromAddress),
                    to: new Twilio.Types.PhoneNumber(toAddress)
                );

                var log = new WhatsAppLog
                {
                    TargetAudience = $"Single: {toPhone}",
                    MessageTemplate = messageText,
                    RecipientCount = 1,
                    SentAt = DateTime.UtcNow,
                    Status = msg.Status.ToString(),
                    LogDetail = JsonSerializer.Serialize(new[] { new { Phone = toPhone, Sid = msg.Sid } })
                };
                _db.WhatsAppLogs.Add(log);
                await _db.SaveChangesAsync();
                return;
            }
        }

        // Mock path
        var logMock = new WhatsAppLog
        {
            TargetAudience = $"Single (Simulated): {toPhone}",
            MessageTemplate = messageText,
            RecipientCount = 1,
            SentAt = DateTime.UtcNow,
            Status = "Simulated",
            LogDetail = JsonSerializer.Serialize(new[] { new { Phone = toPhone, Status = "Simulated" } })
        };
        _db.WhatsAppLogs.Add(logMock);
        await _db.SaveChangesAsync();
    }

    // Task 4.3: Bulk campaign with delay + progress (Req 11.1–11.4)
    public async Task SendBulkWhatsAppCampaignAsync(
        string audienceType,
        string templateText,
        List<ApplicationUser> recipients,
        bool personalize = true,
        IProgress<(int sent, int failed, int total)>? progress = null)
    {
        if (recipients == null || recipients.Count == 0) return;

        var footer = await GetOptOutFooterAsync();

        var delayMs = 1000;
        var delaySetting = await GetSettingAsync("WhatsApp:BulkDelayMs");
        if (int.TryParse(delaySetting, out var parsedDelay) && parsedDelay >= 100 && parsedDelay <= 60000)
            delayMs = parsedDelay;

        var total = recipients.Count;
        var sent = 0;
        var failed = 0;
        var dispatchDetails = new List<object>();

        foreach (var user in recipients)
        {
            var personalizedMsg = templateText;
            if (personalize)
            {
                personalizedMsg = templateText
                    .Replace("{Name}", user.FullName ?? "Client")
                    .Replace("{{Name}}", user.FullName ?? "Client")
                    .Replace("{Phone}", user.PhoneNumber ?? "N/A");
            }
            var messageWithFooter = AppendOptOutFooter(personalizedMsg, footer, out _);

            try
            {
                await SendWhatsAppMessageAsync(user.PhoneNumber ?? "", messageWithFooter);
                sent++;
                dispatchDetails.Add(new { Name = user.FullName, Phone = user.PhoneNumber, Message = messageWithFooter, SentAt = DateTime.UtcNow, Status = "Sent" });
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogError(ex, "Failed to send bulk WhatsApp to {Phone}", user.PhoneNumber);
                dispatchDetails.Add(new { Name = user.FullName, Phone = user.PhoneNumber, Status = "Failed" });
            }

            progress?.Report((sent, failed, total));

            if (delayMs > 0)
                await Task.Delay(delayMs);
        }

        var log = new WhatsAppLog
        {
            TargetAudience = audienceType,
            MessageTemplate = templateText,
            RecipientCount = recipients.Count,
            SentAt = DateTime.UtcNow,
            Status = failed > 0 ? $"Partial ({failed} failed)" : "Sent",
            LogDetail = JsonSerializer.Serialize(dispatchDetails)
        };
        _db.WhatsAppLogs.Add(log);
        await _db.SaveChangesAsync();
    }

    // ==========================================
    // Feedback methods (homepage-enhancement spec)
    // ==========================================

    /// <summary>
    /// Validates rating [1–5], trims comment, sets SubmittedAt to UTC, persists, returns entity.
    /// </summary>
    public async Task<SiteFeedback> SaveFeedbackAsync(int rating, string? comment)
    {
        if (rating < 1 || rating > 5)
            throw new ArgumentOutOfRangeException(nameof(rating), "Rating must be between 1 and 5.");

        var trimmed = comment?.Trim();
        if (trimmed?.Length > 500)
            throw new ArgumentException("Comment must not exceed 500 characters.", nameof(comment));

        var entity = new SiteFeedback
        {
            Rating = rating,
            Comment = string.IsNullOrWhiteSpace(trimmed) ? null : trimmed,
            SubmittedAt = DateTime.UtcNow
        };
        _db.SiteFeedbacks.Add(entity);
        await _db.SaveChangesAsync();
        return entity;
    }

    /// <summary>
    /// Returns a page of feedback records ordered by SubmittedAt descending.
    /// </summary>
    public async Task<List<SiteFeedback>> GetFeedbackAsync(int page = 1, int pageSize = 20)
    {
        return await _db.SiteFeedbacks
            .OrderByDescending(f => f.SubmittedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    /// <summary>
    /// Removes a feedback record by PK; throws KeyNotFoundException if not found.
    /// </summary>
    public async Task DeleteFeedbackAsync(int id)
    {
        var entity = await _db.SiteFeedbacks.FindAsync(id);
        if (entity == null)
            throw new KeyNotFoundException($"SiteFeedback with Id {id} was not found.");
        _db.SiteFeedbacks.Remove(entity);
        await _db.SaveChangesAsync();
    }
}
