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

Local development uses SQLite by default. Production requires MongoDB Atlas; it will fail at startup if the Atlas connection string is missing rather than silently saving to Render's temporary filesystem.

## MongoDB Atlas and Render

Create an Atlas database user with read/write access only to the `protouch` database. In Render, add these environment variables to the web service:

| Variable | Value |
| --- | --- |
| `ConnectionStrings__MongoDb` | The Atlas `mongodb+srv://...` connection string (keep it in Render, never in Git) |
| `MongoDb__DatabaseName` | `protouch` |
| `BootstrapAdmin__Email` | Optional first admin email, used only when the database has no matching account |
| `BootstrapAdmin__Password` | Optional first admin password; choose a strong secret and store it only in Render |

Allow the Render service to connect in Atlas **Network Access**. If you use a broad `0.0.0.0/0` entry because Render's outbound IP can change, keep a strong database-user password and the database user's role limited to this database. Atlas warns when a `/0` address is allowed.

The app stores clinic records and Identity accounts in MongoDB collections. Uploaded profile photos, meal photos, and diet-plan documents are stored in the same Atlas database using GridFS, and file download routes check that the requester owns the file or is an admin.

### Import existing SQLite data once

If you still have a copy of the SQLite database with records you want to keep, first configure the Atlas environment variables locally using .NET user-secrets or your shell. Then run the import from the app's project directory:

```powershell
dotnet run --project ClinicApp\ClinicApp.csproj -- --migrate-sqlite "C:\path\to\app.db"
```

The importer makes a temporary copy of SQLite, applies pending SQLite migrations to that copy, and imports Identity, clinic, booking, feedback, and settings records while preserving IDs. It also copies referenced upload files if those files still exist in `ClinicApp\wwwroot\uploads` or `ClinicApp\App_Data\diet-plan-documents`. The source database is not modified. The Atlas target must be empty; the importer stops if it finds existing documents so it cannot overwrite live data. Review any `Upload not found` messages because files that already disappeared from Render cannot be recovered by a database import.

After deploying, verify that MongoDB Atlas shows the expected collections and that a test record still exists after a Render redeploy. The Atlas free cluster has a 0.5 GB storage cap; backups for free clusters are a separate optional feature and may not be available to every project. Since these records are important clinic and client data, keep a separate backup or use an Atlas tier with managed backups before relying on it for production. [Atlas free-cluster limits](https://www.mongodb.com/docs/atlas/reference/free-shared-limitations/) · [Atlas backup options](https://www.mongodb.com/docs/atlas/backup-restore-cluster/)
