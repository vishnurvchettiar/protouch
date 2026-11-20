using ClinicApp.Services;
using Twilio.Exceptions;

namespace ClinicApp.Tests;

public class TwilioErrorMessageTests
{
    [Theory]
    [InlineData(63015, "sandbox")]
    [InlineData(63016, "24 hours")]
    [InlineData(63007, "WhatsApp sender")]
    [InlineData(21608, "verified")]
    [InlineData(21211, "not valid")]
    public void KnownTwilioCodes_GetPlainLanguageHelp(int code, string expectedPhrase)
    {
        var text = ClinicService.DescribeTwilioError(new ApiException(code, 400, "raw twilio text", ""));
        Assert.Contains(expectedPhrase, text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(code.ToString(), text);
    }

    [Fact]
    public void UnknownTwilioCode_KeepsTwilioMessage_AndCode()
    {
        var text = ClinicService.DescribeTwilioError(new ApiException(99999, 400, "something odd", ""));
        Assert.Contains("something odd", text);
        Assert.Contains("99999", text);
    }

    [Fact]
    public void BadCredentials_AreExplained()
    {
        var text = ClinicService.DescribeTwilioError(new AuthenticationException("Authenticate"));
        Assert.Contains("Account SID", text);
    }

    [Fact]
    public void NonTwilioErrors_PassThrough()
    {
        Assert.Equal("boom", ClinicService.DescribeTwilioError(new InvalidOperationException("boom")));
    }
}
