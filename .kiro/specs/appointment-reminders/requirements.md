# Requirements — WhatsApp Appointment Reminders

## Overview

Clients who have upcoming appointments logged in the ProTouch system receive an automatic WhatsApp reminder **24 hours before** their appointment via the existing Twilio integration. Admins can also trigger reminders manually from the Admin Dashboard. A Twilio configuration guide is embedded in the Admin Settings panel to simplify onboarding.

---

## Requirements

### 1. Background Reminder Service

**1.1** The system MUST run a background worker (`IHostedService`) that checks for upcoming appointments on a regular schedule (every 60 minutes).

**1.2** The worker MUST identify all `Appointment` records where:
- `Status` is `"Scheduled"` or `"Confirmed"`
- `AppointmentDate` is between 23 hours and 25 hours from the current UTC time (a 2-hour window to tolerate scheduling drift)

**1.3** For each matching appointment, the worker MUST send one WhatsApp reminder message to the client's phone number via `ClinicService.SendWhatsAppMessageAsync`.

**1.4** The worker MUST NOT send a second reminder to the same appointment if one has already been sent. A boolean flag `ReminderSent` on the `Appointment` model tracks this.

**1.5** If `WhatsApp:Mode` is `"Mock"`, the worker MUST still execute and log the simulated send (using the existing mock path in `SendWhatsAppMessageAsync`), so the feature is testable without live Twilio credentials.

**1.6** If the client's `PhoneNumber` is null or empty, the worker MUST skip that appointment and log a warning.

**1.7** The worker MUST log each attempt (success or failure) using the injected `ILogger`.

---

### 2. Reminder Message Content

**2.1** The reminder message MUST use the following template:

```
Hello {ClientName}, this is a reminder from ProTouch Physio - Slimming Clinic.

Your appointment for {TreatmentType} is scheduled for tomorrow at {AppointmentTime}.

📍 Row H. No. 22, Kasliwal Bhagyoday, Beed By-pass, Ch. Sambhajinagar.
📞 96735 30031

Reply STOP to unsubscribe.
```

**2.2** `{ClientName}` MUST be replaced with `User.FullName`. If `FullName` is empty, use `"Valued Client"`.

**2.3** `{TreatmentType}` MUST be replaced with `Appointment.TreatmentType`. If empty, use `"your appointment"`.

**2.4** `{AppointmentTime}` MUST be formatted as `"dd MMM yyyy, hh:mm tt"` in Indian Standard Time (UTC+5:30).

---

### 3. Manual Reminder Trigger (Admin Dashboard)

**3.1** The Admin Dashboard's **Appointments** tab MUST show a "Send Reminder" button next to each appointment with `Status` of `"Scheduled"` or `"Confirmed"`.

**3.2** Clicking "Send Reminder" MUST immediately call `ClinicService.SendWhatsAppMessageAsync` with the reminder message for that appointment.

**3.3** After sending, the `ReminderSent` flag MUST be set to `true` and the button MUST change to a disabled "Reminder Sent ✓" state.

**3.4** If the send fails, an inline error message MUST appear next to the appointment row.

**3.5** If the client has no phone number, the "Send Reminder" button MUST be replaced with a greyed-out "No phone" label.

---

### 4. Appointment Model Extension

**4.1** The `Appointment` model MUST gain a `bool ReminderSent` field (default `false`).

**4.2** A new EF Core migration MUST be created to add the `ReminderSent` column to the `Appointments` table.

**4.3** Existing appointments MUST default to `ReminderSent = false` (no backfill needed).

---

### 5. Twilio Setup Guide (Admin Settings)

**5.1** The Admin Dashboard MUST include a **Settings** tab (new tab, last in the tab bar).

**5.2** The Settings tab MUST display:
- Current `WhatsApp:Mode` value (Mock / Twilio) with a toggle to switch between them
- Text inputs for `WhatsApp:Twilio:AccountSid`, `WhatsApp:Twilio:AuthToken`, `WhatsApp:Twilio:From`, and `WhatsApp:ClinicNumber`
- A "Save" button that persists each value to the `Settings` DB table via `ClinicService.SetSettingAsync`

**5.3** The Settings tab MUST include a collapsible **Twilio Setup Guide** panel with the following step-by-step instructions:
1. Create a free Twilio account at [twilio.com](https://twilio.com)
2. From the Twilio Console dashboard, copy your **Account SID** and **Auth Token**
3. Go to **Messaging → Senders → WhatsApp Senders** and join the Twilio Sandbox (send the join code from your WhatsApp)
4. Your Twilio WhatsApp sender number is `whatsapp:+14155238886` (sandbox default)
5. Enter your clinic's WhatsApp number (e.g. `+919028027888`) in the **Clinic Number** field
6. Set **WhatsApp Mode** to `Twilio` and click Save
7. Send a test reminder from the Appointments tab to verify delivery

**5.4** All credential fields (Account SID, Auth Token) MUST render as `type="password"` inputs so values are masked by default.

**5.5** A "Test Connection" button MUST attempt to send a test WhatsApp message to the clinic's own number and display a success or failure result inline.

---

### 6. Logging & Observability

**6.1** Every reminder dispatched by the background worker MUST be recorded in the `WhatsAppLogs` table with `TargetAudience = "Appointment Reminder (Auto)"`.

**6.2** Every manually triggered reminder MUST be recorded with `TargetAudience = "Appointment Reminder (Manual)"`.

**6.3** The WhatsApp Activity log on the Admin Dashboard MUST continue to surface these entries in the "Recent activity" list as it does today.
