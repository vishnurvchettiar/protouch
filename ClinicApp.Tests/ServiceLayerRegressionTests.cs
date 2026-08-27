using System;
using System.Threading.Tasks;
using ClinicApp.Data;
using ClinicApp.Services;
using Xunit;

namespace ClinicApp.Tests;

/// <summary>
/// Regression tests for service layer edge cases not covered by the existing suite.
/// Validates: Requirements 4.2–4.6
/// </summary>
public class ServiceLayerRegressionTests
{
    // Req 4.2 — NormalisePhone(null) must return empty string
    [Fact]
    public void NormalisePhone_NullInput_ReturnsEmpty()
    {
        var result = ClinicService.NormalisePhone(null!);
        Assert.Equal(string.Empty, result);
    }

    // Req 4.3 — SubmitBookingRequestAsync rejects empty ServiceType
    [Fact]
    public async Task SubmitBooking_EmptyServiceType_ThrowsArgumentException()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);

        var request = new BookingRequest
        {
            FullName = "Test Patient",
            PhoneNumber = "9876543210",
            ServiceType = "",
            PreferredDate = DateTime.UtcNow.AddHours(2),
            Notes = ""
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.SubmitBookingRequestAsync(request));
    }

    // Req 4.4 — SubmitBookingRequestAsync rejects empty FullName
    [Fact]
    public async Task SubmitBooking_EmptyFullName_ThrowsArgumentException()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);

        var request = new BookingRequest
        {
            FullName = "",
            PhoneNumber = "9876543210",
            ServiceType = "Physio",
            PreferredDate = DateTime.UtcNow.AddHours(2),
            Notes = ""
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.SubmitBookingRequestAsync(request));
    }

    // Req 4.5 — UpdateDailyLogAsync rejects Weight = 0
    [Fact]
    public async Task UpdateDailyLog_ZeroWeight_ThrowsArgumentException()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);

        var log = new DailyLog
        {
            UserId = "user-zero-weight",
            LogDate = DateTime.Today,
            Weight = 0,
            CarbsPercentage = 25,
            ProteinPercentage = 25,
            FatPercentage = 25,
            VegPercentage = 25,
            LoggedAt = DateTime.UtcNow
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateDailyLogAsync(log));
    }

    // Req 4.5 — UpdateDailyLogAsync rejects negative Weight
    [Fact]
    public async Task UpdateDailyLog_NegativeWeight_ThrowsArgumentException()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);

        var log = new DailyLog
        {
            UserId = "user-neg-weight",
            LogDate = DateTime.Today,
            Weight = -5,
            CarbsPercentage = 25,
            ProteinPercentage = 25,
            FatPercentage = 25,
            VegPercentage = 25,
            LoggedAt = DateTime.UtcNow
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateDailyLogAsync(log));
    }

    // Req 4.6 — UpdateDailyLogAsync rejects LogDate in the future (beyond today)
    [Fact]
    public async Task UpdateDailyLog_FutureDateBeyondToday_ThrowsArgumentException()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);

        var log = new DailyLog
        {
            UserId = "user-future-date",
            LogDate = DateTime.Today.AddDays(1),
            Weight = 70,
            CarbsPercentage = 25,
            ProteinPercentage = 25,
            FatPercentage = 25,
            VegPercentage = 25,
            LoggedAt = DateTime.UtcNow
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateDailyLogAsync(log));
    }
}
