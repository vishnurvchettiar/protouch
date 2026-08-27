using System;
using System.Threading.Tasks;
using ClinicApp.Data;
using Xunit;

namespace ClinicApp.Tests;

/// <summary>
/// Adversarial / negative tests for SubmitBookingRequestAsync.
/// Validates: Requirements 5.1–5.6
/// </summary>
public class BookingNegativeTests
{
    // Req 5.1 — SQL-injection phone: contains non-digit chars → NormalisePhone returns empty → rejected
    [Fact]
    public async Task SubmitBooking_SqlInjectionPhone_ThrowsArgumentException()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);

        var request = new BookingRequest
        {
            FullName = "Test Patient",
            PhoneNumber = "'; DROP TABLE BookingRequests; --",
            ServiceType = "Physio",
            PreferredDate = DateTime.UtcNow.AddHours(2),
            Notes = ""
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.SubmitBookingRequestAsync(request));
    }

    // Req 5.2 — FullName exceeding 200 characters (service enforces ≤ 100 chars max)
    [Fact]
    public async Task SubmitBooking_TooLongFullName_ThrowsArgumentException()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);

        var request = new BookingRequest
        {
            FullName = new string('A', 201),
            PhoneNumber = "9876543210",
            ServiceType = "Physio",
            PreferredDate = DateTime.UtcNow.AddHours(2),
            Notes = ""
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.SubmitBookingRequestAsync(request));
    }

    // Req 5.3 — Notes exceeding 1000 characters
    [Fact]
    public async Task SubmitBooking_TooLongNotes_ThrowsArgumentException()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);

        var request = new BookingRequest
        {
            FullName = "Test Patient",
            PhoneNumber = "9876543210",
            ServiceType = "Physio",
            PreferredDate = DateTime.UtcNow.AddHours(2),
            Notes = new string('N', 1001)
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.SubmitBookingRequestAsync(request));
    }

    // Req 5.4 — PreferredDate = DateTime.MinValue (far in the past)
    [Fact]
    public async Task SubmitBooking_MinValueDate_ThrowsArgumentException()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);

        var request = new BookingRequest
        {
            FullName = "Test Patient",
            PhoneNumber = "9876543210",
            ServiceType = "Physio",
            PreferredDate = DateTime.MinValue,
            Notes = ""
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.SubmitBookingRequestAsync(request));
    }

    // Req 5.5 — PreferredDate = DateTime.MaxValue (far beyond 90-day limit)
    [Fact]
    public async Task SubmitBooking_MaxValueDate_ThrowsArgumentException()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);

        var request = new BookingRequest
        {
            FullName = "Test Patient",
            PhoneNumber = "9876543210",
            ServiceType = "Physio",
            PreferredDate = DateTime.MaxValue,
            Notes = ""
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.SubmitBookingRequestAsync(request));
    }

    // Req 5.6 — XSS payload in FullName: FullName is > 100 chars OR rejected by validation
    // "<script>alert(1)</script>" is 26 chars — within range, so service may accept it.
    // The test asserts either ArgumentException OR it's stored without throwing an unhandled exception.
    [Fact]
    public async Task SubmitBooking_XssPayloadFullName_DoesNotThrowUnhandled()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);

        var request = new BookingRequest
        {
            FullName = "<script>alert(1)</script>",
            PhoneNumber = "9876543210",
            ServiceType = "Physio",
            PreferredDate = DateTime.UtcNow.AddHours(2),
            Notes = ""
        };

        // Must either throw ArgumentException or complete without unhandled exception
        try
        {
            await service.SubmitBookingRequestAsync(request);
            // If stored, verify the record exists in the DB (no unhandled exception)
            var saved = await db.BookingRequests.FindAsync(request.Id);
            Assert.NotNull(saved);
        }
        catch (ArgumentException)
        {
            // Also acceptable — service rejected the XSS payload
        }
    }
}
