using System;
using System.Threading.Tasks;
using ClinicApp.Data;
using Xunit;

namespace ClinicApp.Tests;

/// <summary>
/// Adversarial / negative tests for UpdateDailyLogAsync.
/// Validates: Requirements 6.1–6.5
/// </summary>
public class MealLogNegativeTests
{
    private static DailyLog MakeLog(string userId = "user-test", int carbs = 25, int protein = 25,
        int fat = 25, int veg = 25, double weight = 70, DateTime? date = null) => new()
    {
        UserId = userId,
        LogDate = date ?? DateTime.Today,
        Weight = weight,
        CarbsPercentage = carbs,
        ProteinPercentage = protein,
        FatPercentage = fat,
        VegPercentage = veg,
        LoggedAt = DateTime.UtcNow
    };

    // Req 6.1 — All-zero macros (sum = 0, not 100)
    [Fact]
    public async Task UpdateDailyLog_AllZeroMacros_ThrowsInvalidOperationException()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);
        var log = MakeLog(carbs: 0, protein: 0, fat: 0, veg: 0);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateDailyLogAsync(log));
    }

    // Req 6.2 — Negative carbs (sum would not equal 100)
    [Fact]
    public async Task UpdateDailyLog_NegativeCarbs_ThrowsException()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);
        // -1 + 34 + 34 + 33 = 100 but carbs is negative
        var log = MakeLog(carbs: -1, protein: 34, fat: 34, veg: 33);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateDailyLogAsync(log));
    }

    // Req 6.2 — Negative protein
    [Fact]
    public async Task UpdateDailyLog_NegativeProtein_ThrowsException()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);
        var log = MakeLog(carbs: 34, protein: -1, fat: 34, veg: 33);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateDailyLogAsync(log));
    }

    // Req 6.2 — Negative fat
    [Fact]
    public async Task UpdateDailyLog_NegativeFat_ThrowsException()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);
        var log = MakeLog(carbs: 34, protein: 34, fat: -1, veg: 33);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateDailyLogAsync(log));
    }

    // Req 6.2 — Negative veg
    [Fact]
    public async Task UpdateDailyLog_NegativeVeg_ThrowsException()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);
        var log = MakeLog(carbs: 34, protein: 34, fat: 33, veg: -1);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateDailyLogAsync(log));
    }

    // Req 6.3 — Carbs over 100
    [Fact]
    public async Task UpdateDailyLog_CarbsOver100_ThrowsException()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);
        var log = MakeLog(carbs: 101, protein: 0, fat: 0, veg: 0);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateDailyLogAsync(log));
    }

    // Req 6.4 — Empty UserId
    [Fact]
    public async Task UpdateDailyLog_EmptyUserId_ThrowsException()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);
        var log = MakeLog(userId: "");

        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateDailyLogAsync(log));
    }

    // Req 6.5 — Null UserId
    [Fact]
    public async Task UpdateDailyLog_NullUserId_ThrowsException()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);
        var log = MakeLog(userId: null!);

        await Assert.ThrowsAsync<ArgumentException>(() => service.UpdateDailyLogAsync(log));
    }
}
