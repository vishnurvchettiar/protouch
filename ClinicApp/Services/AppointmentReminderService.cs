using System;
using System.Threading;
using System.Threading.Tasks;
using ClinicApp.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ClinicApp.Services;

/// <summary>
/// Background worker that sends WhatsApp appointment reminders 24 hours before each appointment.
/// Runs every 60 minutes. Implements Requirement 1.1–1.7.
/// </summary>
public class AppointmentReminderService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AppointmentReminderService> _logger;

    public AppointmentReminderService(
        IServiceScopeFactory scopeFactory,
        ILogger<AppointmentReminderService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AppointmentReminderService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            // Delay at the top of the loop so the first tick fires after 60 minutes, not at startup
            try
            {
                await Task.Delay(TimeSpan.FromMinutes(60), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            await RunReminderTickAsync();
        }

        _logger.LogInformation("AppointmentReminderService stopped.");
    }

    private async Task RunReminderTickAsync()
    {
        _logger.LogInformation("AppointmentReminderService: Starting reminder tick at {UtcNow}.", DateTime.UtcNow);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var clinicService = scope.ServiceProvider.GetRequiredService<ClinicService>();

        List<Appointment> appointments;
        try
        {
            appointments = await clinicService.GetUpcomingAppointmentsForReminderAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AppointmentReminderService: Failed to query upcoming appointments.");
            return;
        }

        var sent = 0;
        var skipped = 0;

        foreach (var appt in appointments)
        {
            // Req 1.6: Skip if no phone number
            if (string.IsNullOrWhiteSpace(appt.User?.PhoneNumber))
            {
                _logger.LogWarning(
                    "AppointmentReminderService: Skipping appointment {Id} — client has no phone number.",
                    appt.Id);
                skipped++;
                continue;
            }

            try
            {
                var clientName = appt.User?.FullName ?? string.Empty;
                var message = ClinicService.BuildReminderMessage(
                    clientName,
                    appt.TreatmentType,
                    appt.AppointmentDate);

                // Req 1.3: Send via existing WhatsApp wrapper (respects Mock/Twilio mode)
                await clinicService.SendWhatsAppMessageAsync(appt.User!.PhoneNumber!, message);

                // Log to WhatsAppLogs with Auto audience (Req 6.1)
                await clinicService.LogAppointmentReminderAsync(
                    appt.User.PhoneNumber!,
                    message,
                    isManual: false);

                // Req 1.4: Mark sent so we don't send again
                await clinicService.UpdateAppointmentReminderSentAsync(appt.Id, true);

                _logger.LogInformation(
                    "AppointmentReminderService: Reminder sent for appointment {Id} to {Phone}.",
                    appt.Id, appt.User.PhoneNumber);

                sent++;
            }
            catch (Exception ex)
            {
                // Req 1.7: Log failure but continue to next appointment
                _logger.LogError(ex,
                    "AppointmentReminderService: Failed to send reminder for appointment {Id}.",
                    appt.Id);
                skipped++;
            }
        }

        // Req 3.8: Summary log at end of each tick
        _logger.LogInformation(
            "AppointmentReminderService: Reminder tick complete: {Sent} sent, {Skipped} skipped.",
            sent, skipped);
    }
}
