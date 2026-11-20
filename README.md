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

## Public site design

The public pages (home, services, service detail, booking) use one design system:

- `ClinicApp/wwwroot/ptx.css`: all styles, scoped under a `.px` wrapper so the admin and client dashboards are unaffected.
- `ClinicApp/wwwroot/ptx.js`: GSAP animations and interactions. It falls back to a plain page when GSAP is missing or the visitor prefers reduced motion.
- `ClinicApp/wwwroot/lib/gsap/`: GSAP and ScrollTrigger, served locally (GreenSock standard licence, see the header in each file).
- `ClinicApp/Components/Public/`: shared nav, footer, wave dividers, the service catalogue (`PublicCatalog.cs`) and the interactive pieces (BMI estimator, gallery, feedback, booking forms).
- Pages are server-rendered. Only the forms and widgets are interactive Blazor components, so the animation layer never fights Blazor's DOM updates.

## Where data is saved

Everything the clinic saves is kept in the database: clients, check-ins, meal reviews, diet plans, settings,
bookings, feedback, uploaded meal and profile photos, diet plan documents and the keys that keep people signed in.

- **MongoDB Atlas (production).** Set `MONGODB_URI` in Render. Data stays in Atlas when Render restarts or deploys.
- **PostgreSQL.** `DATABASE_URL` remains supported for services already using PostgreSQL.
- **SQLite (development only).** The local app uses `Data/app.db`; production will not silently fall back to Render's temporary filesystem.

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

Local development uses SQLite by default. Production requires MongoDB Atlas or a configured PostgreSQL database; it will fail at startup rather than silently saving to Render's temporary filesystem.

## MongoDB Atlas and Render

Create an Atlas database user with read/write access only to the `protouch` database. In Render, add these environment variables to the web service:

| Variable | Value |
| --- | --- |
| `MONGODB_URI` | The Atlas `mongodb+srv://...` connection string (keep it in Render, never in Git) |
| `MongoDb__DatabaseName` | Optional; defaults to `protouch` |
| `BootstrapAdmin__Email` | Optional first admin email, used only when the database has no matching account |
| `BootstrapAdmin__Password` | Optional first admin password; choose a strong secret and store it only in Render |

Allow the Render service to connect in Atlas **Network Access**. If you use a broad `0.0.0.0/0` entry because Render's outbound IP can change, keep a strong database-user password and the database user's role limited to this database. Atlas warns when a `/0` address is allowed.

The app stores clinic records, uploaded photos, diet-plan documents, and Identity accounts in MongoDB collections. Private upload routes check that the requester owns the file or is an admin.

### Import existing SQLite data once

If you have a SQLite backup with records you want to keep, configure `MONGODB_URI` locally using .NET user-secrets or your shell, then run the import from the app's project directory:

```powershell
dotnet run --project ClinicApp\ClinicApp.csproj -- --migrate-sqlite "C:\path\to\app.db"
```

The importer makes a temporary copy of SQLite, applies pending SQLite migrations to that copy, and imports Identity, clinic, booking, feedback, settings, and uploaded file records while preserving IDs. The source database is not modified. The Atlas target must be empty; the importer stops if it finds existing documents so it cannot overwrite live data. Render's free service does not provide shell access or persistent disks, so make a verified SQLite backup before switching a live service to MongoDB.

After deploying, verify that MongoDB Atlas shows the expected collections and that a test record still exists after a Render redeploy. The Atlas free cluster has a 0.5 GB storage cap; backups for free clusters are a separate optional feature and may not be available to every project. Since these records are important clinic and client data, keep a separate backup or use an Atlas tier with managed backups before relying on it for production. [Atlas free-cluster limits](https://www.mongodb.com/docs/atlas/reference/free-shared-limitations/) · [Atlas backup options](https://www.mongodb.com/docs/atlas/backup-restore-cluster/)
