using System;
using System.Threading.Tasks;
using ClinicApp.Data;
using Xunit;

namespace ClinicApp.Tests;

/// <summary>
/// Negative tests for UpdateBookingRequestStatusAsync.
/// Validates: Requirements 10.1–10.3
/// </summary>
public class BookingStatusNegativeTests
{
    private static BookingRequest MakeBooking(string status = "Pending") => new()
    {
        FullName = "Test Patient",
        PhoneNumber = "9876543210",
        ServiceType = "Physio",
        PreferredDate = DateTime.UtcNow.AddDays(1),
        Status = status
    };

    // Req 10.1 — Non-existent booking Id throws
    [Fact]
    public async Task UpdateStatus_NonExistentId_ThrowsException()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);

        // Insert a real booking to get a valid Id, then use Id + 9999 which won't exist
        var booking = MakeBooking();
        db.BookingRequests.Add(booking);
        await db.SaveChangesAsync();

        var nonExistentId = booking.Id + 9999;

        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.UpdateBookingRequestStatusAsync(nonExistentId, "Confirmed"));
    }

    // Req 10.2 — Empty toStatus throws InvalidOperationException
    // (empty string is not in valid transitions, so falls through to invalid transition throw)
    [Fact]
    public async Task UpdateStatus_EmptyToStatus_ThrowsException()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);

        var booking = MakeBooking("Pending");
        db.BookingRequests.Add(booking);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateBookingRequestStatusAsync(booking.Id, ""));
    }

    // Req 10.3 — Null toStatus throws
    [Fact]
    public async Task UpdateStatus_NullToStatus_ThrowsException()
    {
        var db = TestDbFactory.CreateInMemoryDb();
        var service = TestDbFactory.CreateClinicService(db);

        var booking = MakeBooking("Pending");
        db.BookingRequests.Add(booking);
        await db.SaveChangesAsync();

        // null is not in the valid transitions set → InvalidOperationException
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.UpdateBookingRequestStatusAsync(booking.Id, null!));
    }
}
