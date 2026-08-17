# Tasks — WhatsApp Appointment Reminders

## Implementation Plan

---

### Task 1 — Extend the Appointment model and create the DB migration

- [ ] 1.1 In `ClinicApp/Data/Models.cs`, add `public bool ReminderSent { get; set; } = false;` to the `Appointment` class
- [ ] 1.2 Run `dotnet ef migrations add AddAppointmentReminderSent --project ClinicApp` to generate the migration
- [ ] 1.3 Verify the generated migration file adds a `ReminderSent` column with a default value of `0`
- [ ] 1.4 Run `dotnet ef database update --project ClinicApp` to apply the migration to the local dev database

**Acceptance:** `Appointment` table has a `ReminderSent` column; existing rows default to `0`.

---

### Task 2 — Add service methods to ClinicService

- [ ] 2.1 Add the `GetIst()` static helper and `IST` static field to `ClinicService.cs` with try/catch for cross-platform timezone resolution
- [ ] 2.2 Add `public static string BuildReminderMessage(string clientName, string treatmentType, DateTime appointmentDateUtc)` to `ClinicService.cs` using the IST timezone and the template from Requirement 2.1
- [ ] 2.3 Add `public async Task UpdateAppointmentReminderSentAsync(int appointmentId, bool sent)` to `ClinicService.cs`
- [ ] 2.4 Add `public async Task<List<Appointment>> GetUpcomingAppointmentsForReminderAsync()` that queries appointments where `Status IN ("Scheduled","Confirmed")`, `ReminderSent = false`, and `AppointmentDate` is between `UtcNow + 23h` and `UtcNow + 25h`, including the `User` navigation property

**Acceptance:** All four methods compile cleanly. `BuildReminderMessage` produces IST-formatted output for a UTC input.

---

### Task 3 — Create the AppointmentReminderService background worker

- [ ] 3.1 Create `ClinicApp/Services/AppointmentReminderService.cs` implementing `BackgroundService` (extends `IHostedService`)
- [ ] 3.2 Constructor takes `IServiceScopeFactory scopeFactory` and `ILogger<AppointmentReminderService> logger`
- [ ] 3.3 In `ExecuteAsync`, loop with `await Task.Delay(TimeSpan.FromMinutes(60), stoppingToken)` at the top of the loop (so the first tick fires after 60 minutes, not at startup)
- [ ] 3.4 Inside each tick, create a new `IServiceScope`, resolve `ApplicationDbContext` and `ClinicService`
- [ ] 3.5 Call `GetUpcomingAppointmentsForReminderAsync()` to get the candidates
- [ ] 3.6 For each appointment: skip if `User?.PhoneNumber` is null/empty (log warning); otherwise build message via `BuildReminderMessage`, call `SendWhatsAppMessageAsync`, then call `UpdateAppointmentReminderSentAsync(id, true)`
- [ ] 3.7 Wrap per-appointment logic in try/catch — log error and continue to the next appointment on failure
- [ ] 3.8 Log a summary at the end of each tick: `"Reminder tick complete: {sent} sent, {skipped} skipped"`
- [ ] 3.9 In `Program.cs`, add `builder.Services.AddHostedService<AppointmentReminderService>();` after the existing `AddScoped<ClinicService>` line

**Acceptance:** App starts without error. In Mock mode, a manually set appointment 24h in the future triggers a simulated WhatsApp log entry after the next tick (can reduce delay to 1 minute for local testing).

---

### Task 4 — Admin Dashboard: Manual reminder buttons on Appointments tab

- [ ] 4.1 In `AdminDashboard.razor`, add `private Dictionary<int, string> reminderErrors = new();` and `private HashSet<int> remindersSent = new();` to the `@code` block
- [ ] 4.2 In `LoadAdminDataAsync`, seed `remindersSent` with IDs of appointments where `appt.ReminderSent == true`
- [ ] 4.3 In the Appointments tab table, add a **Reminder** column header
- [ ] 4.4 For each appointment row, render:
  - If `remindersSent.Contains(appt.Id)` → `<span class="soft-pill">Reminder Sent ✓</span>`
  - Else if `string.IsNullOrWhiteSpace(appt.User?.PhoneNumber)` → `<span class="text-muted small">No phone</span>`
  - Else → `<button class="btn-clinic-secondary" @onclick="() => SendReminderManually(appt)">Send Reminder</button>`
- [ ] 4.5 If `reminderErrors.ContainsKey(appt.Id)`, render `<div class="clinic-alert clinic-alert-danger mt-1 small">@reminderErrors[appt.Id]</div>` below the button
- [ ] 4.6 Add `private async Task SendReminderManually(Appointment appt)` method:
  - Clear any existing error for this ID
  - Build message via `ClinicService.BuildReminderMessage`
  - Call `ClinicService.SendWhatsAppMessageAsync`
  - Call `ClinicService.UpdateAppointmentReminderSentAsync(appt.Id, true)`
  - Add to `remindersSent`; on exception set `reminderErrors[appt.Id]`

**Acceptance:** Send Reminder button appears for eligible appointments. After clicking, button changes to "Reminder Sent ✓". Errors display inline.

---

### Task 5 — Admin Dashboard: Settings tab with Twilio configuration

- [ ] 5.1 Add `"settings"` tab button to the tab bar in `AdminDashboard.razor` (last position)
- [ ] 5.2 Add state variables to `@code`:
  ```
  private string settingsMode = "Mock";
  private string settingsSid = "", settingsToken = "", settingsFrom = "", settingsClinicNum = "";
  private string settingsSaveMsg = "", settingsTestMsg = "";
  private bool settingsSaveSuccess, settingsTestSuccess;
  private bool showTwilioGuide = false;
  ```
- [ ] 5.3 Add `private async Task LoadSettingsAsync()` that loads the four Twilio fields and `WhatsApp:Mode` from `ClinicService.GetSettingAsync` (falls back to empty string if not set)
- [ ] 5.4 Call `LoadSettingsAsync()` from `OnInitializedAsync` (in addition to `LoadAdminDataAsync`)
- [ ] 5.5 Render the settings tab panel with:
  - **Mode toggle:** two buttons ("Mock" / "Twilio") that set `settingsMode`; active mode uses `btn-clinic` class
  - **Credential inputs:** Account SID, Auth Token (both `type="password"`), From number, Clinic WhatsApp number
  - **Save button** → `SaveSettingsAsync()` which calls `SetSettingAsync` for all five keys and shows success message
  - **Test Connection button** → `TestTwilioConnectionAsync()` which sends a test message to `settingsClinicNum` and displays inline pass/fail
- [ ] 5.6 Add a `<details>` collapsible below the credential form with `<summary>📖 Twilio Setup Guide (click to expand)</summary>` containing the 7-step guide from Requirement 5.3, with the Twilio Console link opening in a new tab
- [ ] 5.7 Render `settingsSaveMsg` and `settingsTestMsg` as `clinic-alert` banners (info for success, danger for failure)

**Acceptance:** Settings tab loads existing values. Saving persists to DB. Test Connection shows a Simulated or Twilio result. Guide is readable and collapsible.

---

### Task 6 — Tests

- [ ] 6.1 In `ClinicApp.Tests`, add `AppointmentReminderTests.cs`
- [ ] 6.2 Test `BuildReminderMessage` with a known UTC datetime → assert IST-formatted time appears in the output (e.g. UTC 04:30 → IST 10:00)
- [ ] 6.3 Test `BuildReminderMessage` with empty `clientName` → assert output contains `"Valued Client"`
- [ ] 6.4 Test `BuildReminderMessage` with empty `treatmentType` → assert output contains `"your appointment"`
- [ ] 6.5 Using an in-memory SQLite context, test `GetUpcomingAppointmentsForReminderAsync` returns only appointments in the 23–25h window with `ReminderSent = false` and `Status = "Scheduled"`
- [ ] 6.6 Verify `GetUpcomingAppointmentsForReminderAsync` excludes appointments with `ReminderSent = true`
- [ ] 6.7 Verify `GetUpcomingAppointmentsForReminderAsync` excludes `Status = "Cancelled"` appointments

**Acceptance:** All 6 tests pass with `dotnet test`.
