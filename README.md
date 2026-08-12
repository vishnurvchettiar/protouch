# ProTouch Clinic

ASP.NET Core 9 / Blazor Server clinic management portal.

## Included

- Responsive client portal with progress dashboard, appointments, billing alerts and WhatsApp contact.
- Real profile-photo upload and meal-photo upload to `wwwroot/uploads`.
- Daily check-in focused on weight, meal photo and diet/activity notes. Calorie/macro UI has been removed.
- Admin client directory, client inspector, visit check-in/out and billing.
- Admin **Add User** flow that creates a Client Identity account, Client role and profile in one step.
- Temporary credentials are shown once after client creation and can be sent through a prefilled WhatsApp message.
- WhatsApp-first communication center with audience filters and prefilled `wa.me` links for each recipient.
- Fee reminder, diet reminder and appointment reminder templates.
- WhatsApp activity log. Manual mode does not silently send messages; each message is opened in WhatsApp for the clinic operator to review/send.
- Existing Twilio integration is retained for deployments that want an API gateway.
- `ProTouch.sln` is included.

## Demo credentials

Admin:
- Email: `admin@clinic.com`
- Password: `AdminPass123!`

Seed client examples:
- `sarah@gmail.com` / `ClientPass123!`
- `david@gmail.com` / `ClientPass123!`
- `emma@gmail.com` / `ClientPass123!`

Change all demo passwords before production use.

## WhatsApp setup

Edit `ClinicApp/appsettings.json` and replace:

`WhatsApp:ClinicNumber`

with the clinic's real WhatsApp number in international format, for example `+919876543210`.

The admin WhatsApp center uses the recipient's phone number and opens a prefilled message using `https://wa.me/...`.

For unattended/API-based delivery, configure the existing Twilio settings and set `WhatsApp:Mode` to `Twilio`.

## Run

From the solution directory:

```cmd
dotnet restore
dotnet build ProTouch.sln
dotnet run --project ClinicApp\ClinicApp.csproj
```

The supplied environment used for this preparation does not have the .NET SDK installed, so the final build should be run on the Windows development machine with the .NET 9 SDK installed.
