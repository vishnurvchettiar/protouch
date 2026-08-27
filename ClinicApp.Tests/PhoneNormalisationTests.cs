using ClinicApp.Services;
using Xunit;

namespace ClinicApp.Tests;

/// <summary>
/// Additional phone normalisation edge case tests.
/// Validates: Requirements 8.1–8.2
/// Note: Req 8.3 (idempotency property test) is already covered by
/// NormalisePhone_Idempotent_OnArbitraryInput in PropertyTests.cs.
/// </summary>
public class PhoneNormalisationAdditionalTests
{
    // Req 8.1 — Whitespace-only input returns empty string
    [Theory]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void NormalisePhone_WhitespaceOnly_ReturnsEmpty(string input)
    {
        var result = ClinicService.NormalisePhone(input);
        Assert.Equal(string.Empty, result);
    }

    // Req 8.2 — Full-width Unicode digits are not treated as phone digits
    [Fact]
    public void NormalisePhone_FullWidthDigits_ReturnsEmpty()
    {
        // Full-width Unicode digits: ９８７６５４３２１０ (U+FF19 through U+FF10)
        // char.IsDigit returns false for full-width digits in .NET
        var result = ClinicService.NormalisePhone("９８７６５４３２１０");
        Assert.Equal(string.Empty, result);
    }
}
