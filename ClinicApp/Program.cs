using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;
using System.Security.Claims;
using ClinicApp.Components;
using ClinicApp.Components.Account;
using ClinicApp.Data;

var builder = WebApplication.CreateBuilder(args);

// Required for Render/proxy deployment - forward headers from reverse proxy
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// ── Performance: Brotli + Gzip response compression ──────────────────
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true; // safe: no CRIME attack risk for our static CSS/JS
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
    options.MimeTypes = Microsoft.AspNetCore.ResponseCompression.ResponseCompressionDefaults.MimeTypes.Concat(new[]
    {
        "text/css", "application/javascript", "text/javascript",
        "text/html", "application/json", "image/svg+xml"
    });
});
builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProviderOptions>(options =>
    options.Level = System.IO.Compression.CompressionLevel.Fastest);
builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProviderOptions>(options =>
    options.Level = System.IO.Compression.CompressionLevel.Fastest);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents(options =>
    {
        options.DisconnectedCircuitMaxRetained = 100;
        options.DisconnectedCircuitRetentionPeriod = TimeSpan.FromMinutes(3);
    });

builder.Services.AddSignalR(options =>
{
    options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
    options.HandshakeTimeout = TimeSpan.FromSeconds(15);
    options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    options.MaximumReceiveMessageSize = 32 * 1024; // 32KB
    options.StreamBufferCapacity = 10;
});

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddAuthorization();
builder.Services.AddScoped<IdentityUserAccessor>();
builder.Services.AddScoped<IdentityRedirectManager>();
builder.Services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = IdentityConstants.ApplicationScheme;
        options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
    })
    .AddIdentityCookies();

// Use absolute DB path so the database survives Render restarts/cold starts
var rawConnStr = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Make the SQLite path absolute, relative to ContentRootPath (not CWD)
var connectionString = rawConnStr;
if (rawConnStr.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
{
    var dbRelative = rawConnStr.Substring("Data Source=".Length);
    if (!System.IO.Path.IsPathRooted(dbRelative))
    {
        var dbAbsolute = System.IO.Path.Combine(
            builder.Environment.ContentRootPath, dbRelative);
        // Ensure the directory exists
        var dbDir = System.IO.Path.GetDirectoryName(dbAbsolute)!;
        System.IO.Directory.CreateDirectory(dbDir);
        connectionString = $"Data Source={dbAbsolute}";
    }
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddScoped<ClinicApp.Services.ClinicService>();
builder.Services.AddScoped<ClinicApp.Services.FileUploadService>();
builder.Services.AddScoped<ClinicApp.Services.DietPlanDocumentService>();
builder.Services.AddHostedService<ClinicApp.Services.AppointmentReminderService>();

// Task 14.3: Rate limiter for /book - 5 requests per IP per 60-minute window
builder.Services.AddRateLimiter(options =>
{
    options.AddSlidingWindowLimiter("BookingLimit", limiterOptions =>
    {
        limiterOptions.PermitLimit = 5;
        limiterOptions.Window = TimeSpan.FromMinutes(60);
        limiterOptions.SegmentsPerWindow = 6;
        limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOptions.QueueLimit = 0;
    });
    options.RejectionStatusCode = 429;
});

builder.Services.AddIdentityCore<ApplicationUser>(options => 
    {
        // Security focused defaults
        options.SignIn.RequireConfirmedAccount = false; // Phone OTP is used for verification instead of email
        options.Password.RequireDigit = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = true;
        options.Password.RequireLowercase = true;

        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;

        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// Email sender selection
if (!string.IsNullOrEmpty(builder.Configuration["Smtp:Host"]))
{
    builder.Services.AddSingleton<IEmailSender<ApplicationUser>, SmtpEmailSender>();
}
else
{
    builder.Services.AddSingleton<IEmailSender<ApplicationUser>, IdentityNoOpEmailSender>();
}

var app = builder.Build();

app.UseForwardedHeaders();

// ── Security Headers ─────────────────────────────────────────────────
app.Use(async (context, next) =>
{
    var resp = context.Response.Headers;
    // Prevent clickjacking
    resp["X-Frame-Options"] = "DENY";
    // Prevent MIME type sniffing
    resp["X-Content-Type-Options"] = "nosniff";
    // Legacy XSS protection for older browsers
    resp["X-XSS-Protection"] = "1; mode=block";
    // Control referrer info
    resp["Referrer-Policy"] = "strict-origin-when-cross-origin";
    // Remove server fingerprint
    resp["X-Powered-By"] = "";
    resp.Remove("Server");
    await next();
});

app.UseResponseCompression(); // must be placed after security headers

app.UseRateLimiter();

app.UseWebSockets(new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromSeconds(30)
});

// Seed database
await ClinicApp.Data.DbInitializer.SeedDataAsync(app.Services);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
    // Suppress Blazor SignalR "unload" Permissions-Policy warning in Chrome 115+
    app.Use(async (context, next) =>
    {
        context.Response.Headers["Permissions-Policy"] = "unload=()";
        await next();
    });
}

app.UseHttpsRedirection();


app.UseAntiforgery();

// Task 8.4: Cache-Control for static images (max-age 1 year, immutable)
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        if (ctx.Context.Request.Path.StartsWithSegments("/images"))
        {
            ctx.Context.Response.Headers["Cache-Control"] = "public, max-age=31536000, immutable";
        }
    }
});

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Add additional endpoints required by the Identity /Account Razor components.
app.MapAdditionalIdentityEndpoints();

// Diet plan documents are stored outside wwwroot and only served to the assigned client or an admin.
app.MapGet("/diet-plan-documents/{id:guid}/download", async (
    Guid id,
    ClaimsPrincipal user,
    ApplicationDbContext db,
    ClinicApp.Services.DietPlanDocumentService documents) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrWhiteSpace(userId) || !(user.IsInRole("Admin") || user.IsInRole("Client")))
        return Results.Forbid();

    var document = await db.DietPlanDocuments.AsNoTracking()
        .FirstOrDefaultAsync(item => item.Id == id);
    if (document == null || (!user.IsInRole("Admin") && document.ClientUserId != userId))
        return Results.NotFound();

    var path = documents.GetDownloadPath(document);
    if (path == null)
        return Results.NotFound();

    return Results.File(path, "application/octet-stream", document.OriginalFileName);
})
    .RequireAuthorization();

// PDF diet plans can also be opened in the browser instead of downloaded.
app.MapGet("/diet-plan-documents/{id:guid}/view", async (
    Guid id,
    ClaimsPrincipal user,
    ApplicationDbContext db,
    ClinicApp.Services.DietPlanDocumentService documents) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (string.IsNullOrWhiteSpace(userId) || !(user.IsInRole("Admin") || user.IsInRole("Client")))
        return Results.Forbid();

    var document = await db.DietPlanDocuments.AsNoTracking()
        .FirstOrDefaultAsync(item => item.Id == id);
    if (document == null || (!user.IsInRole("Admin") && document.ClientUserId != userId))
        return Results.NotFound();

    // Only PDFs can be shown in the browser. Word files are downloaded.
    if (!document.OriginalFileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        return Results.Redirect($"/diet-plan-documents/{id}/download");

    var path = documents.GetDownloadPath(document);
    if (path == null)
        return Results.NotFound();

    return Results.File(path, "application/pdf", enableRangeProcessing: true);
})
    .RequireAuthorization();

// Health endpoint — lets Render know the app is alive (avoids cold-start sleep)
app.MapGet("/health", () => Results.Ok(new { status = "ok", time = DateTime.UtcNow }));

app.Run();
