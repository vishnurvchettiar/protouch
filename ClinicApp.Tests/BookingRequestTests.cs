using ClinicApp.Data;

namespace ClinicApp.Tests;

/// <summary>
/// Unit tests for BookingRequest model defaults.
/// Validates: Requirements 14.1
/// </summary>
public class BookingRequestTests
{
    [Fact]
    public void BookingRequest_DefaultStatus_IsPending()
    {
        // Arrange & Act
        var request = new BookingRequest();

        // Assert
        Assert.Equal("Pending", request.Status);
    }

    [Fact]
    public void BookingRequest_DefaultSubmittedAt_IsUtc()
    {
        // Arrange
        var before = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var request = new BookingRequest();

        var after = DateTime.UtcNow.AddSeconds(1);

        // Assert - SubmittedAt should be UTC and between before/after
        Assert.Equal(DateTimeKind.Utc, request.SubmittedAt.Kind);
        Assert.True(request.SubmittedAt >= before && request.SubmittedAt <= after,
            $"SubmittedAt={request.SubmittedAt:O} should be between {before:O} and {after:O}");
    }

    [Fact]
    public void BookingRequest_DefaultStringProperties_AreEmpty()
    {
        var request = new BookingRequest();

        Assert.Equal(string.Empty, request.FullName);
        Assert.Equal(string.Empty, request.PhoneNumber);
        Assert.Equal(string.Empty, request.ServiceType);
        Assert.Equal(string.Empty, request.Notes);
    }
}
