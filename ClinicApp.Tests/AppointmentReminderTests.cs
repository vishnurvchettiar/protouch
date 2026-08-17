using System;
using System.Linq;
using System.Threading.Tasks;
using ClinicApp.Data;
using ClinicApp.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicApp.Tests;

// ============================================================
// Task 6.2: BuildReminderMessage — UTC to IST formatting
// Validates: Requirements 2.1, 2.4
// ============================================================
public class BuildReminderMessageTests
{
    [Fact]
    public void BuildReminderMessage_UtcTime_DisplaysCorrectIstTime()
    {
        // 04:30 UTC = 10:00 IST (UTC+5:30)
        var utcTime = new DateTime(2025, 8, 20, 4, 30, 0, DateTimeKind.Utc);

        var message = ClinicService.BuildReminderMessage("Alice", "Physiotherapy", utcTime);

        // Should contain the IST-formatted time: 10:00 AM
        Assert.Contains("10:00 AM", message);
        // Should also contain the correct date
        Assert.Contains("20 Aug 2025", message);
    }

    [Fact]
    public void BuildReminderMessage_AnotherUtcTime_DisplaysCorrectIstTime()
    {
        // 18:30 UTC = 00:00 IST (midnight, next day)
        var utcTime = new DateTime(2025, 8, 19, 18, 30, 0, DateTimeKind.Utc);

        var message = ClinicService.BuildReminderMessage("Raj", "Slimming Therapy", utcTime);

        // 18:30 UTC + 5:30 = 00:00 IST (20 Aug)
        Assert.Contains("12:00 AM", message);
        Assert.Contains("20 Aug 2025", message);
    }

    // ============================================================
    // Task 6.3: BuildReminderMessage — empty clientName fallback
    // Validates: Requirement 2.2
    // ============================================================
    [Fact]
    public void BuildReminderMessage_EmptyClientName_UsesValuedClient()
    {
        var utcTime = new DateTime(2025, 8, 20, 6, 0, 0, DateTimeKind.Utc);

        var message = ClinicService.BuildReminderMessage(string.Empty, "Physio", utcTime);

        Assert.Contains("Valued Client", message);
    }

    [Fact]
    public void BuildReminderMessage_WhitespaceClientName_UsesValuedClient()
    {
        var utcTime = new DateTime(2025, 8, 20, 6, 0, 0, DateTimeKind.Utc);

        var message = ClinicService.BuildReminderMessage("   ", "Physio", utcTime);

        Assert.Contains("Valued Client", message);
    }

    // ============================================================
    // Task 6.4: BuildReminderMessage — empty treatmentType fallback
    // Validates: Requirement 2.3
    // ============================================================
    [Fact]
    public void BuildReminderMessage_EmptyTreatmentType_UsesYourAppointment()
    {
        var utcTime = new DateTime(2025, 8, 20, 6, 0, 0, DateTimeKind.Utc);

        var message = ClinicService.BuildReminderMessage("Alice", string.Empty, utcTime);

        Assert.Contains("your appointment", message);
    }

    [Fact]
    public void BuildReminderMessage_WhitespaceTreatmentType_UsesYourAppointment()
    {
        var utcTime = new DateTime(2025, 8, 20, 6, 0, 0, DateTimeKind.Utc);

        var message = ClinicService.BuildReminderMessage("Alice", "  ", utcTime);

        Assert.Contains("your appointment", message);
    }

    [Fact]
    public void BuildReminderMessage_ContainsClinicAddress()
    {
        var utcTime = new DateTime(2025, 8, 20, 6, 0, 0, DateTimeKind.Utc);

        var message = ClinicService.BuildReminderMessage("Alice", "Physio", utcTime);

        Assert.Contains("ProTouch Physio", message);
        Assert.Contains("96735 30031", message);
        Assert.Contains("Reply STOP", message);
    }
}

// ============================================================
// Task 6.5: GetUpcomingAppointmentsForReminderAsync — window filter
// Validates: Requirements 1.2, 1.4
// ============================================================
public class GetUpcomingAppointmentsForReminderTests
{
    private static ApplicationUser MakeUser(string id, string phone = "9876543210") =>
        new() { Id = id, UserName = $"{id}@test.com", Email = $"{id}@test.com", FullName = "Test User", PhoneNumber = phone };

    [Fact]
    public async Task GetUpcomingAppointmentsForReminderAsync_ReturnsOnlyAppointmentsInWindow()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);
        var now = DateTime.UtcNow;

        // Add users
        var u1 = MakeUser("u1");
        var u2 = MakeUser("u2");
        var u3 = MakeUser("u3");
        db.Users.AddRange(u1, u2, u3);

        // In window: 24h from now
        var inWindow = new Appointment
        {
            UserId = "u1", Status = "Scheduled", ReminderSent = false,
            AppointmentDate = now.AddHours(24), TreatmentType = "Physio"
        };
        // Too soon: 22h
        var tooSoon = new Appointment
        {
            UserId = "u2", Status = "Scheduled", ReminderSent = false,
            AppointmentDate = now.AddHours(22), TreatmentType = "Physio"
        };
        // Too far: 26h
        var tooFar = new Appointment
        {
            UserId = "u3", Status = "Scheduled", ReminderSent = false,
            AppointmentDate = now.AddHours(26), TreatmentType = "Physio"
        };

        db.Appointments.AddRange(inWindow, tooSoon, tooFar);
        await db.SaveChangesAsync();

        var result = await service.GetUpcomingAppointmentsForReminderAsync();

        Assert.Single(result);
        Assert.Equal(inWindow.Id, result[0].Id);
    }

    [Fact]
    public async Task GetUpcomingAppointmentsForReminderAsync_IncludesBothWindowEdges()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);
        var now = DateTime.UtcNow;

        var u1 = MakeUser("u1"); var u2 = MakeUser("u2");
        db.Users.AddRange(u1, u2);

        // Exactly at 23h boundary
        var atStart = new Appointment
        {
            UserId = "u1", Status = "Scheduled", ReminderSent = false,
            AppointmentDate = now.AddHours(23).AddMinutes(1), TreatmentType = "Physio"
        };
        // Exactly at 25h boundary
        var atEnd = new Appointment
        {
            UserId = "u2", Status = "Confirmed", ReminderSent = false,
            AppointmentDate = now.AddHours(24).AddMinutes(59), TreatmentType = "Physio"
        };

        db.Appointments.AddRange(atStart, atEnd);
        await db.SaveChangesAsync();

        var result = await service.GetUpcomingAppointmentsForReminderAsync();

        Assert.Equal(2, result.Count);
    }

    // ============================================================
    // Task 6.6: GetUpcomingAppointmentsForReminderAsync — excludes ReminderSent=true
    // Validates: Requirement 1.4
    // ============================================================
    [Fact]
    public async Task GetUpcomingAppointmentsForReminderAsync_ExcludesAlreadySentReminders()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);
        var now = DateTime.UtcNow;

        var u1 = MakeUser("u1"); var u2 = MakeUser("u2");
        db.Users.AddRange(u1, u2);

        var alreadySent = new Appointment
        {
            UserId = "u1", Status = "Scheduled", ReminderSent = true,
            AppointmentDate = now.AddHours(24), TreatmentType = "Physio"
        };
        var notSentYet = new Appointment
        {
            UserId = "u2", Status = "Scheduled", ReminderSent = false,
            AppointmentDate = now.AddHours(24), TreatmentType = "Slimming"
        };

        db.Appointments.AddRange(alreadySent, notSentYet);
        await db.SaveChangesAsync();

        var result = await service.GetUpcomingAppointmentsForReminderAsync();

        Assert.Single(result);
        Assert.Equal(notSentYet.Id, result[0].Id);
    }

    // ============================================================
    // Task 6.7: GetUpcomingAppointmentsForReminderAsync — excludes Cancelled
    // Validates: Requirement 1.2
    // ============================================================
    [Fact]
    public async Task GetUpcomingAppointmentsForReminderAsync_ExcludesCancelledAppointments()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);
        var now = DateTime.UtcNow;

        var u1 = MakeUser("u1");
        var u2 = MakeUser("u2");
        var u3 = MakeUser("u3");
        db.Users.AddRange(u1, u2, u3);

        var cancelled = new Appointment
        {
            UserId = "u1", Status = "Cancelled", ReminderSent = false,
            AppointmentDate = now.AddHours(24), TreatmentType = "Physio"
        };
        var completed = new Appointment
        {
            UserId = "u2", Status = "Completed", ReminderSent = false,
            AppointmentDate = now.AddHours(24), TreatmentType = "Physio"
        };
        var scheduled = new Appointment
        {
            UserId = "u3", Status = "Scheduled", ReminderSent = false,
            AppointmentDate = now.AddHours(24), TreatmentType = "Slimming"
        };

        db.Appointments.AddRange(cancelled, completed, scheduled);
        await db.SaveChangesAsync();

        var result = await service.GetUpcomingAppointmentsForReminderAsync();

        Assert.Single(result);
        Assert.Equal("Scheduled", result[0].Status);
    }

    [Fact]
    public async Task GetUpcomingAppointmentsForReminderAsync_IncludesConfirmedStatus()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);
        var now = DateTime.UtcNow;

        var u1 = MakeUser("u1");
        db.Users.Add(u1);

        var confirmed = new Appointment
        {
            UserId = "u1", Status = "Confirmed", ReminderSent = false,
            AppointmentDate = now.AddHours(24), TreatmentType = "Physio"
        };

        db.Appointments.Add(confirmed);
        await db.SaveChangesAsync();

        var result = await service.GetUpcomingAppointmentsForReminderAsync();

        Assert.Single(result);
        Assert.Equal("Confirmed", result[0].Status);
    }
}
