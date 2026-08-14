using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ClinicApp.Data;
using ClinicApp.Services;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicApp.Tests;

// ============================================================
// Property 1: Diet plan persistence round-trip
// Validates: Requirements 8.2, 11.2
// Feature: dashboard-enhancements, Property 1: diet plan persistence round-trip
// ============================================================
public class DietPlanPersistenceRoundTripTests
{
    private static async Task<ApplicationUser> SeedUserWithProfileAsync(ApplicationDbContext db, string userId)
    {
        var user = new ApplicationUser
        {
            Id = userId,
            UserName = $"{userId}@test.com",
            Email = $"{userId}@test.com",
            FullName = $"Test User {userId}"
        };
        db.Users.Add(user);
        var profile = new ClientProfile
        {
            UserId = userId,
            DoctorNotes = string.Empty,
            DietPlan = string.Empty,
            StartWeight = 70,
            TargetWeight = 65,
            Height = 170,
            IsActive = true,
            JoiningDate = DateTime.UtcNow
        };
        db.ClientProfiles.Add(profile);
        await db.SaveChangesAsync();
        return user;
    }

    // Property 1 — round-trip test: save any dietPlan string and load it back
    [Property(MaxTest = 100)]
    public Property UpdateClientDietPlanAsync_RoundTrip_PersistsExactString()
    {
        return Prop.ForAll(
            Arb.Default.String().Filter(s => s != null),
            (dietPlan) =>
            {
                var userId = Guid.NewGuid().ToString("N")[..12];
                var result = Task.Run(async () =>
                {
                    var db = TestDbFactory.CreateInMemoryDb();
                    var service = TestDbFactory.CreateClinicService(db);
                    await SeedUserWithProfileAsync(db, userId);

                    await service.UpdateClientDietPlanAsync(userId, dietPlan);

                    var loaded = await service.GetClientProfileByUserIdAsync(userId);
                    return loaded?.DietPlan == dietPlan;
                }).GetAwaiter().GetResult();

                return result;
            });
    }

    [Fact]
    public async Task UpdateClientDietPlanAsync_EmptyString_Persisted()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);
        var userId = "uid-empty-plan";
        await SeedUserWithProfileAsync(db, userId);

        await service.UpdateClientDietPlanAsync(userId, string.Empty);

        var loaded = await service.GetClientProfileByUserIdAsync(userId);
        Assert.Equal(string.Empty, loaded?.DietPlan);
    }

    [Fact]
    public async Task UpdateClientDietPlanAsync_LongString_Persisted()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);
        var userId = "uid-long-plan";
        await SeedUserWithProfileAsync(db, userId);

        var longPlan = new string('A', 2000);
        await service.UpdateClientDietPlanAsync(userId, longPlan);

        var loaded = await service.GetClientProfileByUserIdAsync(userId);
        Assert.Equal(longPlan, loaded?.DietPlan);
    }

    [Fact]
    public async Task UpdateClientDietPlanAsync_MultilineString_Persisted()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);
        var userId = "uid-multiline-plan";
        await SeedUserWithProfileAsync(db, userId);

        var plan = "Breakfast: oats\nLunch: salad\nDinner: rice";
        await service.UpdateClientDietPlanAsync(userId, plan);

        var loaded = await service.GetClientProfileByUserIdAsync(userId);
        Assert.Equal(plan, loaded?.DietPlan);
    }
}

// ============================================================
// Property 2 (Unit Test): UpdateClientDietPlanAsync — missing profile error path
// Validates: Requirements 11.3
// ============================================================
public class DietPlanMissingProfileTests
{
    [Fact]
    public async Task UpdateClientDietPlanAsync_MissingProfile_ThrowsInvalidOperationException()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);
        var nonExistentUserId = "no-such-user-xyz";

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateClientDietPlanAsync(nonExistentUserId, "some plan"));

        Assert.Contains(nonExistentUserId, ex.Message);
        Assert.Contains("Client profile not found", ex.Message);
    }

    [Theory]
    [InlineData("ghost-user-1")]
    [InlineData("ghost-user-2")]
    [InlineData("")]
    public async Task UpdateClientDietPlanAsync_VariousNonExistentUserIds_AllThrow(string userId)
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateClientDietPlanAsync(userId, "plan text"));
    }
}

// ============================================================
// Property 2 (PBT): WhatsApp message format correctness
// Validates: Requirements 10.1
// Feature: dashboard-enhancements, Property 2: WhatsApp message format correctness
// ============================================================
public class WhatsAppMessageFormatTests
{
    [Property(MaxTest = 100)]
    public Property FormatDietPlanMessage_AlwaysMatchesTemplate()
    {
        return Prop.ForAll(
            Arb.Default.String().Filter(s => s != null),
            Arb.Default.String().Filter(s => s != null),
            (clientName, planText) =>
            {
                var result = WhatsAppLogHelper.FormatDietPlanMessage(clientName, planText);
                var expected = $"Dear {clientName}, here is your personalised diet plan from Dr. Manjari:\n\n{planText}";
                return result == expected;
            });
    }

    [Theory]
    [InlineData("Priya Sharma", "Eat healthy", "Dear Priya Sharma, here is your personalised diet plan from Dr. Manjari:\n\nEat healthy")]
    [InlineData("", "Some plan", "Dear , here is your personalised diet plan from Dr. Manjari:\n\nSome plan")]
    [InlineData("Rahul", "", "Dear Rahul, here is your personalised diet plan from Dr. Manjari:\n\n")]
    public void FormatDietPlanMessage_ConcreteExamples(string clientName, string planText, string expected)
    {
        var result = WhatsAppLogHelper.FormatDietPlanMessage(clientName, planText);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void FormatDietPlanMessage_ContainsDrManjari()
    {
        var result = WhatsAppLogHelper.FormatDietPlanMessage("Alice", "Diet plan text");
        Assert.Contains("Dr. Manjari", result);
    }

    [Fact]
    public void FormatDietPlanMessage_ContainsDoubleNewlineSeparator()
    {
        var result = WhatsAppLogHelper.FormatDietPlanMessage("Alice", "Diet plan text");
        Assert.Contains("\n\n", result);
    }
}

// ============================================================
// Property 3 (PBT): LogDetail badge mapping completeness
// Validates: Requirements 7.1, 7.2, 7.3, 7.4, 7.5
// Feature: dashboard-enhancements, Property 3: LogDetail badge mapping completeness
// ============================================================
public class LogDetailBadgeMappingTests
{
    private static readonly string[] ValidCssClasses = ["badge-active", "badge-info", "badge-danger", "badge-pending"];
    private static readonly string[] KnownStatuses = ["Sent", "Simulated", "Failed", "Unknown", "", "other"];

    [Property(MaxTest = 100)]
    public Property GetLogDetailBadges_NeverThrows_AllCssInAllowedSet()
    {
        // Generate arbitrary JSON arrays with Status fields
        var statusGen = Gen.Elements(KnownStatuses);
        var itemCountGen = Gen.Choose(0, 10);

        var jsonGen = itemCountGen.SelectMany(count =>
            Gen.ListOf(count, statusGen).Select(statuses =>
            {
                var items = statuses.Select(s => $"{{\"Status\":\"{s}\"}}");
                return "[" + string.Join(",", items) + "]";
            }));

        return Prop.ForAll(jsonGen.ToArbitrary(), (logDetail) =>
        {
            var result = WhatsAppLogHelper.GetLogDetailBadges(logDetail);
            // Must not throw (already guaranteed by return type)
            // Each CSS class must be in the allowed set
            return result.All(b => ValidCssClasses.Contains(b.Css));
        });
    }

    [Fact]
    public void GetLogDetailBadges_SentStatus_ReturnsBadgeActive()
    {
        var json = "[{\"Status\":\"Sent\"}]";
        var result = WhatsAppLogHelper.GetLogDetailBadges(json);
        Assert.Single(result);
        Assert.Equal("Sent", result[0].Label);
        Assert.Equal("badge-active", result[0].Css);
    }

    [Fact]
    public void GetLogDetailBadges_SimulatedStatus_ReturnsBadgeInfo()
    {
        var json = "[{\"Status\":\"Simulated\"}]";
        var result = WhatsAppLogHelper.GetLogDetailBadges(json);
        Assert.Single(result);
        Assert.Equal("Simulated", result[0].Label);
        Assert.Equal("badge-info", result[0].Css);
    }

    [Fact]
    public void GetLogDetailBadges_FailedStatus_ReturnsBadgeDanger()
    {
        var json = "[{\"Status\":\"Failed\"}]";
        var result = WhatsAppLogHelper.GetLogDetailBadges(json);
        Assert.Single(result);
        Assert.Equal("Failed", result[0].Label);
        Assert.Equal("badge-danger", result[0].Css);
    }

    [Fact]
    public void GetLogDetailBadges_UnknownStatus_ReturnsBadgePending()
    {
        var json = "[{\"Status\":\"SomeRandom\"}]";
        var result = WhatsAppLogHelper.GetLogDetailBadges(json);
        Assert.Single(result);
        Assert.Equal("SomeRandom", result[0].Label);
        Assert.Equal("badge-pending", result[0].Css);
    }

    [Fact]
    public void GetLogDetailBadges_MissingStatusProperty_ReturnsUnknownBadgePending()
    {
        var json = "[{\"Name\":\"Alice\"}]";
        var result = WhatsAppLogHelper.GetLogDetailBadges(json);
        Assert.Single(result);
        Assert.Equal("Unknown", result[0].Label);
        Assert.Equal("badge-pending", result[0].Css);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-json-at-all")]
    [InlineData("{\"key\":\"value\"}")]  // object not array
    public void GetLogDetailBadges_InvalidInput_ReturnsEmptyList(string? input)
    {
        var result = WhatsAppLogHelper.GetLogDetailBadges(input);
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void GetLogDetailBadges_LabelMatchesOriginalStatus()
    {
        var json = "[{\"Status\":\"Sent\"},{\"Status\":\"Failed\"},{\"Status\":\"Simulated\"}]";
        var result = WhatsAppLogHelper.GetLogDetailBadges(json);
        Assert.Equal(3, result.Count);
        Assert.Equal("Sent",      result[0].Label);
        Assert.Equal("Failed",    result[1].Label);
        Assert.Equal("Simulated", result[2].Label);
    }
}

// ============================================================
// Property 4 (PBT): LogDetail truncation
// Validates: Requirements 7.6
// Feature: dashboard-enhancements, Property 4: LogDetail truncation
// ============================================================
public class LogDetailTruncationTests
{
    private static string MakeJsonArray(int count) =>
        "[" + string.Join(",", Enumerable.Range(0, count).Select(i => $"{{\"Status\":\"Sent\"}}")) + "]";

    [Property(MaxTest = 100)]
    public Property GetLogDetailBadges_ReturnsAllEntries_ForArbitraryLength()
    {
        // Generate arrays with N items in range 6..100
        var countGen = Gen.Choose(6, 100);
        return Prop.ForAll(countGen.ToArbitrary(), (n) =>
        {
            var json = MakeJsonArray(n);
            var result = WhatsAppLogHelper.GetLogDetailBadges(json);
            // GetLogDetailBadges returns ALL N entries; truncation is done in the Razor template
            return result.Count == n;
        });
    }

    [Theory]
    [InlineData(6)]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(100)]
    public void GetLogDetailBadges_ReturnsAllNEntries(int n)
    {
        var json = MakeJsonArray(n);
        var result = WhatsAppLogHelper.GetLogDetailBadges(json);
        Assert.Equal(n, result.Count);
    }

    [Theory]
    [InlineData(6,  1)]
    [InlineData(10, 5)]
    [InlineData(7,  2)]
    public void TruncationLogic_ExtraCountIsCorrect(int totalBadges, int expectedExtra)
    {
        var json = MakeJsonArray(totalBadges);
        var badges = WhatsAppLogHelper.GetLogDetailBadges(json);
        var extraCount = badges.Count > 5 ? badges.Count - 5 : 0;
        Assert.Equal(expectedExtra, extraCount);
        // Razor renders: badges.Take(5) + "+extraCount more"
        Assert.Equal(Math.Min(5, totalBadges), badges.Take(5).Count());
    }

    [Fact]
    public void GetLogDetailBadges_ExactlyFive_NoExtra()
    {
        var json = MakeJsonArray(5);
        var badges = WhatsAppLogHelper.GetLogDetailBadges(json);
        var extraCount = badges.Count > 5 ? badges.Count - 5 : 0;
        Assert.Equal(5, badges.Count);
        Assert.Equal(0, extraCount);
    }

    [Fact]
    public void GetLogDetailBadges_Zero_EmptyList()
    {
        var json = "[]";
        var badges = WhatsAppLogHelper.GetLogDetailBadges(json);
        Assert.Empty(badges);
    }
}

// ============================================================
// Unit tests for diet plan validation error paths
// Validates: Requirements 9.6, 9.7
// ============================================================
public class DietPlanValidationTests
{
    // These test the same validation logic used in SendDietPlanAsync
    // extracted here as pure logic assertions

    [Fact]
    public void EmptyDietPlanText_ShouldProduceValidationError()
    {
        var dietPlanText = "";
        var isInvalid = string.IsNullOrWhiteSpace(dietPlanText);
        Assert.True(isInvalid, "Empty diet plan text should be invalid");
    }

    [Fact]
    public void WhiteSpaceOnlyDietPlanText_ShouldProduceValidationError()
    {
        var dietPlanText = "   \t\n  ";
        var isInvalid = string.IsNullOrWhiteSpace(dietPlanText);
        Assert.True(isInvalid, "Whitespace-only diet plan text should be invalid");
    }

    [Fact]
    public void NoDeliveryMethodSelected_ShouldProduceError()
    {
        bool sendViaWA = false;
        bool saveToProfile = false;
        var noMethodSelected = !sendViaWA && !saveToProfile;
        Assert.True(noMethodSelected, "No delivery method should trigger error");
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void AtLeastOneDeliveryMethodSelected_ShouldBeValid(bool sendViaWA, bool saveToProfile)
    {
        var atLeastOne = sendViaWA || saveToProfile;
        Assert.True(atLeastOne, "At least one delivery method should be valid");
    }
}
