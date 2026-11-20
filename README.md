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

- **PostgreSQL (use this on Render).** Set the `DATABASE_URL` environment variable and the app uses PostgreSQL.
  Data lives on the database server, so redeploying or restarting the web service never loses anything.
- **SQLite (local development).** With no `DATABASE_URL` the app uses the SQLite file `Data/app.db`, as before.

### Moving to PostgreSQL on Render

1. In Render create a **PostgreSQL** database. Use a paid instance: Render's free PostgreSQL is deleted after 30 days.
   Turn on the backups your plan offers.
2. Open the database page and copy its **Internal Database URL**.
3. Open the web service, Environment, and add `DATABASE_URL` with that value. Save and deploy.
4. **Keep the old Disk attached for this first deploy.** On start-up the app copies the existing SQLite data
   (`/var/data/app.db`) and any photo or diet plan files into PostgreSQL, checks that every table has the same
   number of rows, and only then commits. The old file is never changed or deleted. The log shows
   "Imported N rows from SQLite into PostgreSQL". The import runs once, only into an empty PostgreSQL database.
5. After you have checked the site, the Disk is no longer needed.

If the import finds a problem it rolls everything back and the app refuses to start, so the previous
working deployment stays live and nothing is half-copied. To import a SQLite file from somewhere else, set
`IMPORT_SQLITE_PATH` to its path.

### Changing the database model

There are two migration sets, one per database: `Data/Migrations` (SQLite) and `Data/MigrationsPg` (PostgreSQL).
After changing a model, add a migration to both (tests fail if one is missing):

```bash
dotnet ef migrations add Name --context ApplicationDbContext   --output-dir Data/Migrations
dotnet ef migrations add Name --context PgApplicationDbContext --output-dir Data/MigrationsPg
```

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
