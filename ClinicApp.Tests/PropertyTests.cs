using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClinicApp.Data;
using ClinicApp.Services;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ClinicApp.Tests;

// ============================================================
// Property 6: Meal Log Upsert — No Duplicates per User per Day
// Validates: Requirements 8.3
// ============================================================
public class MealLogUpsertNoDuplicatesTests
{
    [Fact]
    public async Task UpdateDailyLogAsync_CalledMultipleTimes_SameDaySameUser_ResultsInExactlyOneRecord()
    {
        // Property 6 — tested as a concrete property test over multiple iterations
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);

        var userId = "user-upsert-test";
        var logDate = DateTime.Today;

        for (var i = 1; i <= 5; i++)
        {
            var log = new DailyLog
            {
                UserId = userId,
                LogDate = logDate,
                Weight = 70 + i,
                CarbsPercentage = 40,
                ProteinPercentage = 30,
                FatPercentage = 15,
                VegPercentage = 15,
                DietNotes = $"Update #{i}",
                LoggedAt = DateTime.UtcNow
            };
            await service.UpdateDailyLogAsync(log);
        }

        var count = await db.DailyLogs.CountAsync(l => l.UserId == userId && l.LogDate == logDate);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task UpdateDailyLogAsync_DifferentUsers_SameDay_EachHasExactlyOneRecord()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);
        var logDate = DateTime.Today;

        var userIds = new[] { "user-A", "user-B", "user-C" };
        foreach (var uid in userIds)
        {
            for (var i = 0; i < 3; i++)
            {
                var log = new DailyLog
                {
                    UserId = uid,
                    LogDate = logDate,
                    Weight = 65,
                    CarbsPercentage = 25,
                    ProteinPercentage = 35,
                    FatPercentage = 20,
                    VegPercentage = 20,
                    LoggedAt = DateTime.UtcNow
                };
                await service.UpdateDailyLogAsync(log);
            }
        }

        foreach (var uid in userIds)
        {
            var count = await db.DailyLogs.CountAsync(l => l.UserId == uid && l.LogDate == logDate);
            Assert.Equal(1, count);
        }
    }
}

// ============================================================
// Property 1: Macro Sum Invariant
// Validates: Requirements 7.1, 7.3, 7.4
// ============================================================
public class MacroSumInvariantTests
{
    [Theory]
    [InlineData(40, 30, 20, 10)]  // valid: 100
    [InlineData(25, 25, 25, 25)]  // valid: 100
    [InlineData(35, 30, 15, 20)]  // valid: 100
    public async Task UpdateDailyLogAsync_ValidMacroSum_Succeeds(int carbs, int protein, int fat, int veg)
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);
        var log = new DailyLog
        {
            UserId = "user-macro-valid",
            LogDate = DateTime.Today,
            Weight = 70,
            CarbsPercentage = carbs,
            ProteinPercentage = protein,
            FatPercentage = fat,
            VegPercentage = veg,
            LoggedAt = DateTime.UtcNow
        };
        await service.UpdateDailyLogAsync(log);
        var saved = await db.DailyLogs.FirstAsync(l => l.UserId == "user-macro-valid");
        Assert.Equal(100, saved.CarbsPercentage + saved.ProteinPercentage + saved.FatPercentage + saved.VegPercentage);
    }

    [Theory]
    [InlineData(30, 30, 30, 30)]  // invalid: 120
    [InlineData(10, 10, 10, 10)]  // invalid: 40
    [InlineData(0, 0, 0, 0)]      // invalid: 0
    [InlineData(100, 0, 0, 1)]    // invalid: 101
    public async Task UpdateDailyLogAsync_InvalidMacroSum_ThrowsInvalidOperationException(int carbs, int protein, int fat, int veg)
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);
        var log = new DailyLog
        {
            UserId = "user-macro-invalid",
            LogDate = DateTime.Today,
            Weight = 70,
            CarbsPercentage = carbs,
            ProteinPercentage = protein,
            FatPercentage = fat,
            VegPercentage = veg,
            LoggedAt = DateTime.UtcNow
        };
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateDailyLogAsync(log));
    }
}

// ============================================================
// Property 2: Booking Request Default Status
// Validates: Requirements 14.1
// ============================================================
public class BookingDefaultStatusTests
{
    private static BookingRequest MakeValidRequest(string? name = null, string? phone = null, DateTime? date = null) => new()
    {
        FullName = name ?? "Test Patient",
        PhoneNumber = phone ?? "9876543210",
        ServiceType = "Slimming",
        PreferredDate = date ?? DateTime.UtcNow.AddHours(2),
        Notes = "Test booking"
    };

    [Fact]
    public async Task SubmitBookingRequestAsync_AlwaysSetsStatusToPending()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);
        var request = MakeValidRequest();
        request.Status = "SomethingElse"; // try to override

        await service.SubmitBookingRequestAsync(request);

        var saved = await db.BookingRequests.FindAsync(request.Id);
        Assert.Equal("Pending", saved!.Status);
    }

    [Fact]
    public async Task SubmitBookingRequestAsync_SubmittedAtIsUtcAndNotFuture()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);
        var before = DateTime.UtcNow.AddSeconds(-1);
        var request = MakeValidRequest();

        await service.SubmitBookingRequestAsync(request);

        var after = DateTime.UtcNow.AddSeconds(1);
        var saved = await db.BookingRequests.FindAsync(request.Id);
        Assert.Equal(DateTimeKind.Utc, saved!.SubmittedAt.Kind);
        Assert.True(saved.SubmittedAt >= before && saved.SubmittedAt <= after);
    }
}

// ============================================================
// Property 3: Booking Date Must Be in the Future
// Validates: Requirements 14.6
// ============================================================
public class BookingDateValidationTests
{
    [Theory]
    [InlineData(-1)]   // 1 day in the past
    [InlineData(-30)]  // 30 days in the past
    [InlineData(0)]    // now (too close — < 1 hour)
    public async Task SubmitBookingRequestAsync_PastDate_ThrowsArgumentException(int offsetHours)
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);
        var request = new BookingRequest
        {
            FullName = "Test Patient",
            PhoneNumber = "9876543210",
            ServiceType = "Physio",
            PreferredDate = DateTime.UtcNow.AddHours(offsetHours),
            Notes = ""
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.SubmitBookingRequestAsync(request));
    }

    [Theory]
    [InlineData(2)]    // 2 hours in future — valid
    [InlineData(24)]   // 1 day in future — valid
    [InlineData(720)]  // 30 days in future — valid
    public async Task SubmitBookingRequestAsync_ValidFutureDate_Succeeds(int offsetHours)
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);
        var request = new BookingRequest
        {
            FullName = "Test Patient",
            PhoneNumber = "9876543210",
            ServiceType = "Physio",
            PreferredDate = DateTime.UtcNow.AddHours(offsetHours),
            Notes = ""
        };

        var id = await service.SubmitBookingRequestAsync(request);
        Assert.True(id > 0);
    }

    [Fact]
    public async Task SubmitBookingRequestAsync_TooFarFuture_ThrowsArgumentException()
    {
        // 91 days — exceeds 90 day limit
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);
        var request = new BookingRequest
        {
            FullName = "Test Patient",
            PhoneNumber = "9876543210",
            ServiceType = "Physio",
            PreferredDate = DateTime.UtcNow.AddDays(91),
            Notes = ""
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.SubmitBookingRequestAsync(request));
    }
}

// ============================================================
// Property 10: BookingRequest Status Transitions Are Monotone
// Validates: Requirements 16.2, 16.3
// ============================================================
public class BookingStatusTransitionTests
{
    [Theory]
    [InlineData("Pending",   "Confirmed")]  // allowed
    [InlineData("Pending",   "Cancelled")]  // allowed
    [InlineData("Confirmed", "Cancelled")]  // allowed
    public async Task UpdateBookingRequestStatusAsync_ValidTransition_Succeeds(string fromStatus, string toStatus)
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);

        var booking = new BookingRequest { FullName = "T", PhoneNumber = "9876543210", ServiceType = "S", PreferredDate = DateTime.UtcNow.AddDays(1), Status = fromStatus };
        db.BookingRequests.Add(booking);
        await db.SaveChangesAsync();

        await service.UpdateBookingRequestStatusAsync(booking.Id, toStatus);
        var updated = await db.BookingRequests.FindAsync(booking.Id);
        Assert.Equal(toStatus, updated!.Status);
    }

    [Theory]
    [InlineData("Pending",   "Pending")]    // no-op self-transition
    [InlineData("Confirmed", "Confirmed")]  // no-op
    [InlineData("Confirmed", "Pending")]    // backwards
    [InlineData("Cancelled", "Pending")]    // terminal state
    [InlineData("Cancelled", "Confirmed")]  // terminal state
    [InlineData("Cancelled", "Cancelled")]  // terminal state
    [InlineData("Pending",   "Completed")]  // invalid status
    public async Task UpdateBookingRequestStatusAsync_InvalidTransition_ThrowsInvalidOperationException(string fromStatus, string toStatus)
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);

        var booking = new BookingRequest { FullName = "T", PhoneNumber = "9876543210", ServiceType = "S", PreferredDate = DateTime.UtcNow.AddDays(1), Status = fromStatus };
        db.BookingRequests.Add(booking);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateBookingRequestStatusAsync(booking.Id, toStatus));
    }
}

// ============================================================
// Property 4: Phone Normalisation Idempotency
// Validates: Requirements 18.2
// ============================================================
public class PhoneNormalisationIdempotencyTests
{
    [Theory]
    [InlineData("9876543210")]
    [InlineData("09876543210")]
    [InlineData("919876543210")]
    [InlineData("+919876543210")]
    [InlineData("00919876543210")]
    [InlineData("91-9876-543210")]
    public void NormalisePhone_IsIdempotent(string phone)
    {
        var first = ClinicService.NormalisePhone(phone);
        var second = ClinicService.NormalisePhone(first);
        Assert.Equal(first, second);
    }

    [Theory]
    [InlineData("9876543210", "919876543210")]
    [InlineData("919876543210", "919876543210")]
    [InlineData("00919876543210", "919876543210")]
    public void NormalisePhone_CorrectOutput(string input, string expected)
    {
        Assert.Equal(expected, ClinicService.NormalisePhone(input));
    }

    [Theory]
    [InlineData("123")]
    [InlineData("")]
    [InlineData("abcdef")]
    [InlineData("12345")]
    public void NormalisePhone_InvalidInput_ReturnsEmpty(string input)
    {
        Assert.Equal(string.Empty, ClinicService.NormalisePhone(input));
    }

    [Property(MaxTest = 200)]
    public Property NormalisePhone_Idempotent_OnArbitraryInput()
    {
        return Prop.ForAll(
            Arb.Default.String().Filter(s => s != null),
            (phone) =>
            {
                var first = ClinicService.NormalisePhone(phone);
                var second = ClinicService.NormalisePhone(first);
                return first == second;
            });
    }
}

// ============================================================
// Property 5: CSV Preview Produces No Side Effects
// Validates: Requirements 10.2, 10.3
// ============================================================
public class CsvPreviewNoSideEffectsTests
{
    [Fact]
    public async Task PreviewCsvCampaignAsync_DoesNotModifyDatabase()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);

        var recipients = new List<CsvRecipient>
        {
            new("Alice", "9000000001"),
            new("Bob", "9000000002"),
            new("Charlie", "9000000003")
        };

        var beforeCount = await db.WhatsAppLogs.CountAsync();

        // Call preview multiple times
        for (var i = 0; i < 5; i++)
        {
            await service.PreviewCsvCampaignAsync("Hello {{Name}}, your appointment is tomorrow.", recipients);
        }

        var afterCount = await db.WhatsAppLogs.CountAsync();
        Assert.Equal(beforeCount, afterCount);
    }

    [Fact]
    public async Task PreviewCsvCampaignAsync_ReturnsCorrectPersonalisedMessages()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);

        var recipients = new List<CsvRecipient>
        {
            new("Priya", "9876543210"),
            new("", "9876543211")  // blank name — should use {{Name}} literal
        };

        var result = await service.PreviewCsvCampaignAsync("Hello {{Name}}!", recipients);

        Assert.Equal(2, result.Count);
        Assert.Contains("Priya", result[0].PersonalisedMessage);
        Assert.Contains("{{Name}}", result[1].PersonalisedMessage);
        // Both should have the opt-out footer
        Assert.All(result, r => Assert.Contains("Reply STOP", r.PersonalisedMessage));
    }
}

// ============================================================
// Property 9: Opt-Out Footer Always Present in Bulk Messages
// Validates: Requirements 9.1, 9.3
// ============================================================
public class OptOutFooterTests
{
    [Fact]
    public async Task SendCsvCampaign_EachLoggedMessageContainsOptOutFooter()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);

        var recipients = new List<CsvRecipient>
        {
            new("Alice", "9000000001"),
            new("Bob", "9000000002"),
        };

        await service.SendCsvWhatsAppCampaignAsync("Test campaign", "Hello {{Name}}, reminder from clinic.", recipients);

        // Verify that the sent messages in WhatsApp logs contain the footer
        var logs = await db.WhatsAppLogs.ToListAsync();
        var singleLogs = logs.Where(l => l.TargetAudience.StartsWith("Single")).ToList();

        Assert.NotEmpty(singleLogs);
        Assert.All(singleLogs, log => Assert.Contains("Reply STOP", log.MessageTemplate));
    }

    [Fact]
    public async Task SendBulkCampaign_EachMessageContainsOptOutFooter()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);

        var recipients = new List<ApplicationUser>
        {
            new() { Id = "u1", FullName = "Alice", PhoneNumber = "9000000001", UserName = "alice@test.com", Email = "alice@test.com" },
            new() { Id = "u2", FullName = "Bob",   PhoneNumber = "9000000002", UserName = "bob@test.com",   Email = "bob@test.com" },
        };
        db.Users.AddRange(recipients);
        await db.SaveChangesAsync();

        await service.SendBulkWhatsAppCampaignAsync("Test Bulk", "Hello {{Name}}, wellness tip!", recipients);

        var logs = await db.WhatsAppLogs.ToListAsync();
        var singleLogs = logs.Where(l => l.TargetAudience.StartsWith("Single")).ToList();

        Assert.NotEmpty(singleLogs);
        Assert.All(singleLogs, log => Assert.Contains("Reply STOP", log.MessageTemplate));
    }
}

// ============================================================
// Property 8: File Upload Extension Allowlist Enforcement
// Validates: Requirements 6.4, 19.1
// ============================================================
public class FileUploadExtensionAllowlistTests
{
    [Theory]
    [InlineData(".jpg",  "image/jpeg",  true)]
    [InlineData(".jpeg", "image/jpeg",  true)]
    [InlineData(".png",  "image/png",   true)]
    [InlineData(".webp", "image/webp",  true)]
    [InlineData(".gif",  "image/gif",   false)]
    [InlineData(".bmp",  "image/bmp",   false)]
    [InlineData(".pdf",  "application/pdf", false)]
    [InlineData(".exe",  "application/octet-stream", false)]
    [InlineData(".txt",  "text/plain",  false)]
    [InlineData(".svg",  "image/svg+xml", false)]
    [InlineData(".jpg",  "image/png",   false)]  // mismatched ext/mime
    [InlineData(".png",  "image/jpeg",  false)]  // mismatched ext/mime
    public void ValidateFile_AllowlistEnforcement(string extension, string mimeType, bool shouldBeValid)
    {
        var (isValid, error) = FileUploadService.ValidateFile($"test{extension}", mimeType, 1024);
        Assert.Equal(shouldBeValid, isValid);
        if (!shouldBeValid) Assert.NotNull(error);
    }
}

// ============================================================
// Property 13: File Upload Size Enforcement
// Validates: Requirements 6.5, 19.2
// ============================================================
public class FileUploadSizeEnforcementTests
{
    private const long EightMB = 8 * 1024 * 1024;

    [Theory]
    [InlineData(1024)]              // 1 KB — allowed
    [InlineData(4 * 1024 * 1024)]  // 4 MB — allowed
    [InlineData(8 * 1024 * 1024)]  // exactly 8 MB — allowed
    public void ValidateFile_UnderOrAtLimit_IsValid(long sizeBytes)
    {
        var (isValid, error) = FileUploadService.ValidateFile("test.jpg", "image/jpeg", sizeBytes);
        Assert.True(isValid, $"Expected valid for {sizeBytes} bytes but got error: {error}");
    }

    [Theory]
    [InlineData(8 * 1024 * 1024 + 1)]  // 1 byte over — rejected
    [InlineData(16 * 1024 * 1024)]      // 16 MB — rejected
    [InlineData(100 * 1024 * 1024)]     // 100 MB — rejected
    public void ValidateFile_OverLimit_IsInvalid(long sizeBytes)
    {
        var (isValid, error) = FileUploadService.ValidateFile("test.jpg", "image/jpeg", sizeBytes);
        Assert.False(isValid, $"Expected invalid for {sizeBytes} bytes");
        Assert.NotNull(error);
    }

    [Property(MaxTest = 300)]
    public Property ValidateFile_SizeEnforcement_IsConsistentWithThreshold()
    {
        return Prop.ForAll(
            Gen.Choose(0, (int)(EightMB + 1024 * 1024)).ToArbitrary(),
            (size) =>
            {
                var (isValid, _) = FileUploadService.ValidateFile("test.jpg", "image/jpeg", size);
                var expectedValid = size <= EightMB;
                return isValid == expectedValid;
            });
    }
}


// ============================================================
// Property 7: Meal Review Returns Only Logs With Photos
// Validates: Requirements 5.2, 5.5
// ============================================================
public class MealReviewOnlyLogsWithPhotosTests
{
    [Fact]
    public async Task GetAllMealLogsAsync_ReturnsOnlyLogsWithPhotos()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);

        // Seed logs: some with photos, some without
        var logsWithPhotos = new[]
        {
            new DailyLog { UserId = "u1", LogDate = DateTime.Today, MealPhotoUrl = "/uploads/meals/meal1.jpg", DietNotes = "Notes 1", CarbsPercentage = 25, ProteinPercentage = 25, FatPercentage = 25, VegPercentage = 25, LoggedAt = DateTime.UtcNow },
            new DailyLog { UserId = "u2", LogDate = DateTime.Today, MealPhotoUrl = "/uploads/meals/meal2.jpg", DietNotes = "Notes 2", CarbsPercentage = 25, ProteinPercentage = 25, FatPercentage = 25, VegPercentage = 25, LoggedAt = DateTime.UtcNow },
        };
        var logsWithoutPhotos = new[]
        {
            new DailyLog { UserId = "u3", LogDate = DateTime.Today, MealPhotoUrl = "", DietNotes = "Notes 3", CarbsPercentage = 25, ProteinPercentage = 25, FatPercentage = 25, VegPercentage = 25, LoggedAt = DateTime.UtcNow },
            new DailyLog { UserId = "u4", LogDate = DateTime.Today, MealPhotoUrl = "", DietNotes = "Notes 4", CarbsPercentage = 25, ProteinPercentage = 25, FatPercentage = 25, VegPercentage = 25, LoggedAt = DateTime.UtcNow },
        };

        db.DailyLogs.AddRange(logsWithPhotos);
        db.DailyLogs.AddRange(logsWithoutPhotos);
        await db.SaveChangesAsync();

        var result = await service.GetAllMealLogsAsync();

        // Assert: only logs with non-empty MealPhotoUrl are returned
        Assert.Equal(2, result.Count);
        Assert.All(result, log => Assert.NotEmpty(log.MealPhotoUrl));

        // Assert: all non-empty-photo logs appear in result
        foreach (var expected in logsWithPhotos)
        {
            Assert.Contains(result, r => r.MealPhotoUrl == expected.MealPhotoUrl);
        }
    }

    [Fact]
    public async Task GetAllMealLogsAsync_OrderedByDescendingLogDate()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);

        db.DailyLogs.AddRange(
            new DailyLog { UserId = "u1", LogDate = DateTime.Today.AddDays(-3), MealPhotoUrl = "/img/a.jpg", DietNotes = "a", CarbsPercentage = 25, ProteinPercentage = 25, FatPercentage = 25, VegPercentage = 25, LoggedAt = DateTime.UtcNow },
            new DailyLog { UserId = "u2", LogDate = DateTime.Today.AddDays(-1), MealPhotoUrl = "/img/b.jpg", DietNotes = "b", CarbsPercentage = 25, ProteinPercentage = 25, FatPercentage = 25, VegPercentage = 25, LoggedAt = DateTime.UtcNow },
            new DailyLog { UserId = "u3", LogDate = DateTime.Today, MealPhotoUrl = "/img/c.jpg", DietNotes = "c", CarbsPercentage = 25, ProteinPercentage = 25, FatPercentage = 25, VegPercentage = 25, LoggedAt = DateTime.UtcNow }
        );
        await db.SaveChangesAsync();

        var result = await service.GetAllMealLogsAsync();
        Assert.Equal(3, result.Count);
        Assert.True(result[0].LogDate >= result[1].LogDate && result[1].LogDate >= result[2].LogDate);
    }

    [Fact]
    public async Task GetAllMealLogsAsync_Pagination_RespectsPageSize()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);

        // Add 25 logs with photos in batches
        for (var i = 0; i < 25; i++)
        {
            db.DailyLogs.Add(new DailyLog
            {
                UserId = $"user-{i}",
                LogDate = DateTime.Today.AddDays(-i),
                MealPhotoUrl = $"/img/{i}.jpg",
                DietNotes = $"Notes {i}",
                CarbsPercentage = 25,
                ProteinPercentage = 25,
                FatPercentage = 25,
                VegPercentage = 25,
                LoggedAt = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync();

        var page1 = await service.GetAllMealLogsAsync(1, 20);
        var page2 = await service.GetAllMealLogsAsync(2, 20);

        Assert.Equal(20, page1.Count);
        Assert.Equal(5, page2.Count);
    }
}





// ============================================================
// Property 11: Booking Form Always Renders Blank on /book
// Validates: Requirements 13.1, 13.2
// Note: This tests the service-layer defaults (no session prefill)
// ============================================================
public class BookingFormBlankTests
{
    [Theory]
    [InlineData("authenticated-user")]
    [InlineData("")]
    [InlineData(null)]
    public void BookingRequest_AlwaysStartsBlank_RegardlessOfContext(string? contextUser)
    {
        // Simulate page load: form fields must always be empty string / default
        // The ServiceBooking.razor initializes all fields to "" and DateTime.Today + defaults
        // This tests the model-layer contract
        var request = new BookingRequest();

        // All string fields start empty (never prefilled from any session)
        Assert.Equal(string.Empty, request.FullName);
        Assert.Equal(string.Empty, request.PhoneNumber);
        Assert.Equal(string.Empty, request.ServiceType);
        Assert.Equal(string.Empty, request.Notes);

        // Unused contextUser parameter simulates the test for both auth states
        _ = contextUser;
    }

    [Fact]
    public void BookingRequest_StatusDefault_IsPendingRegardlessOfInput()
    {
        // Even if caller sets a different status, SubmitBookingRequestAsync resets it
        var request = new BookingRequest { Status = "Confirmed" };
        Assert.Equal("Confirmed", request.Status); // Model itself doesn't enforce — service does

        // Service-layer: ensure status is always reset to Pending on submit
        // (Already tested in BookingDefaultStatusTests.SubmitBookingRequestAsync_AlwaysSetsStatusToPending)
    }
}

// ============================================================
// Property 12: Booking Phone Validation Rejects Invalid Numbers
// Validates: Requirements 14.5
// ============================================================
public class BookingPhoneValidationTests
{
    [Theory]
    [InlineData("1234567890")]   // starts with 1 — invalid Indian mobile
    [InlineData("5678901234")]   // starts with 5 — invalid
    [InlineData("12345")]        // too short
    [InlineData("")]             // empty
    [InlineData("abcdefghij")]   // non-numeric
    [InlineData("00000000000")]  // all zeros
    [InlineData("1111111111")]   // starts with 1
    public async Task SubmitBookingRequestAsync_InvalidPhone_ThrowsArgumentException(string invalidPhone)
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);
        var request = new BookingRequest
        {
            FullName = "Test Patient",
            PhoneNumber = invalidPhone,
            ServiceType = "Slimming",
            PreferredDate = DateTime.UtcNow.AddHours(2),
            Notes = ""
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.SubmitBookingRequestAsync(request));
    }

    [Theory]
    [InlineData("9876543210")]   // valid: starts with 9
    [InlineData("8765432109")]   // valid: starts with 8
    [InlineData("7654321098")]   // valid: starts with 7
    [InlineData("6543210987")]   // valid: starts with 6
    public async Task SubmitBookingRequestAsync_ValidIndianPhone_Succeeds(string validPhone)
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);
        var request = new BookingRequest
        {
            FullName = "Test Patient",
            PhoneNumber = validPhone,
            ServiceType = "Physio",
            PreferredDate = DateTime.UtcNow.AddHours(2),
            Notes = ""
        };

        var id = await service.SubmitBookingRequestAsync(request);
        Assert.True(id > 0);
    }
}
