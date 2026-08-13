using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ClinicApp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace ClinicApp.Services;

public record CsvRecipient(string Name, string Phone);

public class ClinicService
{
    private readonly ApplicationDbContext _db;
    private readonly IConfiguration _config;

    public ClinicService(ApplicationDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
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
        foreach (var profile in profiles)
            profile.ProfilePhotoUrl = await GetClientPhotoAsync(profile.UserId) ?? string.Empty;
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

    public async Task EnsureClientProfileExistsAsync(string userId, double startWeight = 70.0, double targetWeight = 65.0, double height = 170.0)
    {
        var exists = await _db.ClientProfiles.AnyAsync(cp => cp.UserId == userId);
        if (!exists)
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
    }


    public async Task<string?> GetClientPhotoAsync(string userId)
    {
        return await GetSettingAsync($"ClientPhoto:{userId}");
    }

    public async Task SetClientPhotoAsync(string userId, string relativePath)
    {
        await SetSettingAsync($"ClientPhoto:{userId}", relativePath);
    }

    public string BuildWhatsAppLink(string phone, string message)
    {
        var normalized = new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());
        if (normalized.StartsWith("00")) normalized = normalized[2..];
        if (normalized.Length > 12 && normalized.StartsWith("91")) normalized = normalized[2..];
        if (normalized.Length == 10) normalized = "91" + normalized;
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

    public async Task UpdateDailyLogAsync(DailyLog log)
    {
        var existing = await _db.DailyLogs.FindAsync(log.Id);
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
            existing.LoggedAt = log.LoggedAt;
            await _db.SaveChangesAsync();
        }
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

    public async Task SendCsvWhatsAppCampaignAsync(string campaignName, string template, IEnumerable<CsvRecipient> recipients)
    {
        var sent = 0;
        foreach (var recipient in recipients.Where(r => !string.IsNullOrWhiteSpace(r.Phone)))
        {
            var message = template.Replace("{{Name}}", recipient.Name ?? "Client").Replace("{Name}", recipient.Name ?? "Client");
            await SendWhatsAppMessageAsync(recipient.Phone, message);
            sent++;
        }
        _db.WhatsAppLogs.Add(new WhatsAppLog { TargetAudience = campaignName, MessageTemplate = template, RecipientCount = sent, Status = "Bulk campaign dispatched", LogDetail = "CSV broadcast" });
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

            // Simulate WhatsApp sending immediately
            string phone = invoice.User?.PhoneNumber ?? "9999999999";
            string clientName = invoice.User?.FullName ?? "Client";
            string msg = $"Hello {clientName}, this is an automated reminder from Slimming & Physio Clinic. Your payment of {invoice.Amount:C} is due for: {invoice.Description}. Please settle as soon as possible.";
            
            // Create a WhatsApp dispatch log
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
    // Phone OTP flows
    // ==========================================
    public async Task<string> GeneratePhoneOtpAsync(string phone)
    {
        // Check for recent OTP to prevent rapid resends (60 seconds)
        var recent = await _db.PhoneOtps
            .Where(o => o.PhoneNumber == phone && !o.IsUsed)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync();

        if (recent != null && (DateTime.UtcNow - recent.CreatedAt).TotalSeconds < 60)
        {
            // reuse recent code
            return recent.Code;
        }

        // create 6-digit code
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
                // log
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
    // WhatsApp sending wrapper (bulk)
    // ==========================================
    public async Task SendWhatsAppMessageAsync(string toPhone, string messageText)
    {
        var mode = GetWhatsAppMode();
        if (mode == "Twilio")
        {
            var cfg = GetTwilioConfig();
            if (!string.IsNullOrEmpty(cfg.Sid) && !string.IsNullOrEmpty(cfg.Token))
            {
                TwilioClient.Init(cfg.Sid, cfg.Token);
                var msg = MessageResource.Create(
                    body: messageText,
                    from: new Twilio.Types.PhoneNumber(cfg.From),
                    to: new Twilio.Types.PhoneNumber(toPhone)
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

    // Provide a bulk helper that uses SendWhatsAppMessageAsync
    public async Task SendBulkWhatsAppCampaignAsync(string audienceType, string templateText, List<ApplicationUser> recipients, bool personalize = true)
    {
        if (recipients == null || recipients.Count == 0) return;

        var dispatchDetails = new List<object>();

        foreach (var user in recipients)
        {
            var personalizedMsg = templateText;
            if (personalize)
            {
                personalizedMsg = templateText
                    .Replace("{Name}", user.FullName ?? "Client")
                    .Replace("{Phone}", user.PhoneNumber ?? "N/A");
            }

            await SendWhatsAppMessageAsync(user.PhoneNumber ?? "", personalizedMsg);

            dispatchDetails.Add(new
            {
                Name = user.FullName,
                Phone = user.PhoneNumber,
                Message = personalizedMsg,
                SentAt = DateTime.UtcNow
            });
        }

        var log = new WhatsAppLog
        {
            TargetAudience = audienceType,
            MessageTemplate = templateText,
            RecipientCount = recipients.Count,
            SentAt = DateTime.UtcNow,
            Status = "Sent",
            LogDetail = JsonSerializer.Serialize(dispatchDetails)
        };
        _db.WhatsAppLogs.Add(log);
        await _db.SaveChangesAsync();
    }
}
