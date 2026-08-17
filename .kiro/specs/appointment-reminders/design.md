# Design — WhatsApp Appointment Reminders

## Architecture Overview

The feature adds three new components to the existing Blazor Server app:

1. **`AppointmentReminderService`** — an `IHostedService` background worker registered as a singleton
2. **`ReminderSent` flag** — a bool column added to the `Appointments` table via EF Core migration
3. **Admin Settings tab** — a new tab on `AdminDashboard.razor` for Twilio credentials + setup guide

All WhatsApp sending continues to flow through the existing `ClinicService.SendWhatsAppMessageAsync`, which already handles the Mock/Twilio switch via `WhatsApp:Mode`.

---

## Component Design

### 1. Background Worker — `AppointmentReminderService`

**File:** `ClinicApp/Services/AppointmentReminderService.cs`

```
IHostedService
  └─ ExecuteAsync(CancellationToken)
       ├─ loop: await Task.Delay(60 minutes)
       ├─ using IServiceScope → resolve ApplicationDbContext + ClinicService
       ├─ query Appointments WHERE Status IN ("Scheduled","Confirmed")
       │                         AND ReminderSent = false
       │                         AND AppointmentDate BETWEEN (now+23h, now+25h)
       ├─ for each → build message → SendWhatsAppMessageAsync → set ReminderSent = true
       └─ SaveChangesAsync
```

**Why a scoped service inside a singleton?** `AppointmentReminderService` is a singleton (required by `IHostedService`), but `ApplicationDbContext` and `ClinicService` are scoped. We resolve them freshly inside each timer tick using `IServiceScopeFactory`, which is the standard ASP.NET Core pattern for background workers using EF Core.

**Registration in `Program.cs`:**
```csharp
builder.Services.AddHostedService<AppointmentReminderService>();
```

**Error handling:** Any exception during a single appointment's send is caught and logged; the loop continues to the next appointment. The `ReminderSent` flag is only set after a successful send.

---

### 2. Model Extension — `Appointment.ReminderSent`

**File:** `ClinicApp/Data/Models.cs`

Add to `Appointment`:
```csharp
public bool ReminderSent { get; set; } = false;
```

**Migration:** A new EF Core migration adds the `ReminderSent` column with a default of `0` (SQLite boolean). No data backfill is needed — existing appointments default to `false`, meaning the worker may send them a reminder if they fall within the 23–25 hour window, which is the correct behaviour.

---

### 3. Message Builder — `ClinicService.BuildReminderMessage`

A new static/instance method added to `ClinicService`:

```csharp
public static string BuildReminderMessage(string clientName, string treatmentType, DateTime appointmentDateUtc)
{
    var ist = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
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
```

**Timezone note:** `"Asia/Kolkata"` is the IANA timezone ID, which works on Linux (Render deployment). For Windows dev environments, `"India Standard Time"` is the Windows ID. We use `TimeZoneInfo.FindSystemTimeZoneById` with a try/catch fallback to handle both:

```csharp
private static readonly TimeZoneInfo IST = GetIst();
private static TimeZoneInfo GetIst()
{
    try { return TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"); }
    catch { return TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"); }
}
```

---

### 4. Admin Dashboard — Manual Reminder & Settings Tab

#### 4a. Appointments Tab Changes (`AdminDashboard.razor`)

The existing appointments table gains a **Send Reminder** button column:

```
| Client | Treatment | Date | Status | [existing] | Reminder |
```

- **Scheduled / Confirmed + phone present:** `<button>Send Reminder</button>`
- **After send:** `<span class="soft-pill">Reminder Sent ✓</span>` (button replaced)
- **No phone:** `<span class="text-muted small">No phone</span>`
- **On error:** `<div class="clinic-alert clinic-alert-danger">@reminderError</div>` inline

State tracking uses a `Dictionary<int, string> reminderErrors` and a `HashSet<int> remindersSent` (seeded from `appt.ReminderSent` on load).

#### 4b. New Settings Tab

New tab button added to the tab bar:
```html
<button class="btn @(activeTab == "settings" ? "btn-clinic" : "btn-clinic-secondary")"
        @onclick='() => activeTab = "settings"'>Settings</button>
```

Settings tab content sections:

**WhatsApp Mode toggle:**
```
[Mock] [Twilio]   ← two buttons acting as a toggle
```

**Credential fields (all `type="password"`):**
- Account SID
- Auth Token
- From number (e.g. `whatsapp:+14155238886`)
- Clinic WhatsApp number (e.g. `+919028027888`)

**Save button** → calls `ClinicService.SetSettingAsync` for each field.

**Test Connection button** → calls `ClinicService.SendWhatsAppMessageAsync(clinicNumber, "✅ ProTouch test message from Kiro setup.")` and shows inline result.

**Twilio Setup Guide** — a collapsible `<details>` element with the 7-step guide from Requirement 5.3.

---

### 5. Data Flow Diagram

```
[Timer tick every 60 min]
        │
        ▼
AppointmentReminderService
        │  IServiceScopeFactory.CreateScope()
        │  → resolve ApplicationDbContext
        │  → query Appointments (Status=Scheduled/Confirmed, ReminderSent=false, 23h–25h window)
        │
        ├─ for each appointment:
        │       │  resolve ClinicService (scoped)
        │       │  appt.User.PhoneNumber → skip if empty
        │       │  BuildReminderMessage(name, treatment, date)
        │       │  SendWhatsAppMessageAsync(phone, message)
        │       │     └─ WhatsApp:Mode=Twilio → TwilioClient.MessageResource.Create(...)
        │       │     └─ WhatsApp:Mode=Mock  → WhatsAppLog (Simulated)
        │       │  appt.ReminderSent = true
        │       │  SaveChangesAsync()
        │       └─ log success/failure
        │
[Admin clicks "Send Reminder" on Appointments tab]
        │
        ▼
AdminDashboard.razor
        │  ClinicService.SendWhatsAppMessageAsync(phone, message)
        │  ClinicService.UpdateAppointmentReminderSentAsync(id, true)  ← new helper
        └─ remindersSent.Add(appt.Id) → UI updates to "Reminder Sent ✓"
```

---

### 6. New Service Method — `UpdateAppointmentReminderSentAsync`

Added to `ClinicService` for the manual-trigger path:

```csharp
public async Task UpdateAppointmentReminderSentAsync(int appointmentId, bool sent)
{
    var appt = await _db.Appointments.FindAsync(appointmentId);
    if (appt == null) return;
    appt.ReminderSent = sent;
    await _db.SaveChangesAsync();
}
```

---

### 7. Files Changed / Created

| File | Change |
|------|--------|
| `Data/Models.cs` | Add `ReminderSent` bool to `Appointment` |
| `Data/Migrations/` | New migration: `AddAppointmentReminderSent` |
| `Services/AppointmentReminderService.cs` | **New** — background worker |
| `Services/ClinicService.cs` | Add `BuildReminderMessage`, `UpdateAppointmentReminderSentAsync` |
| `Program.cs` | Register `AddHostedService<AppointmentReminderService>()` |
| `Components/Pages/AdminDashboard.razor` | Add Settings tab + manual reminder buttons on Appointments tab |

---

### 8. Correctness Properties

| # | Property | Testable? |
|---|----------|-----------|
| P1 | `BuildReminderMessage` produces IST-formatted time from UTC input | Yes — unit test |
| P2 | Worker queries only appointments in the 23–25h window | Yes — unit test with in-memory DB |
| P3 | Worker skips appointments with `ReminderSent = true` | Yes — unit test |
| P4 | Worker skips appointments with empty phone | Yes — unit test |
| P5 | Manual send sets `ReminderSent = true` in DB | Yes — integration test |
| P6 | Settings save persists all 4 Twilio fields to the `Settings` table | Yes — integration test |
