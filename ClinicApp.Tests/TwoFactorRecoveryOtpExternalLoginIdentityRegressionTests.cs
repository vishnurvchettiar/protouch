using System.IO;
using Xunit;

namespace ClinicApp.Tests;

/// <summary>
/// Regression tests for the 2FA, recovery-code, phone-OTP, and external-login
/// pages (LoginWith2fa.razor, LoginWithRecoveryCode.razor, VerifyPhone.razor,
/// ExternalLogin.razor) migrated onto AuthLayout and the pt-auth-* canonical
/// CSS class family. Ensures the layout migration preserves existing Identity
/// behavior and the Requirement 4.1/4.2 heading-level fixes remain in place.
/// Validates: Requirements 4.1, 4.2, 5.4, 5.5, 5.6, 5.7
/// </summary>
public class TwoFactorRecoveryOtpExternalLoginIdentityRegressionTests
{
    private static readonly string SolutionRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));

    private static readonly string AccountPagesDir =
        Path.Combine(SolutionRoot, "ClinicApp/Components/Account/Pages");

    private static readonly string LoginWith2faPath =
        Path.Combine(AccountPagesDir, "LoginWith2fa.razor");

    private static readonly string LoginWithRecoveryCodePath =
        Path.Combine(AccountPagesDir, "LoginWithRecoveryCode.razor");

    private static readonly string VerifyPhonePath =
        Path.Combine(AccountPagesDir, "VerifyPhone.razor");

    private static readonly string ExternalLoginPath =
        Path.Combine(AccountPagesDir, "ExternalLogin.razor");

    // Requirements 1.1, 5.4 — LoginWith2fa.razor declares @layout AuthLayout,
    // preserves its FormName literal, and still calls TwoFactorAuthenticatorSignInAsync
    [Fact]
    public void LoginWith2fa_PreservesIdentityBehavior()
    {
        var content = File.ReadAllText(LoginWith2faPath);

        Assert.Contains("@layout AuthLayout", content);
        Assert.Contains("FormName=\"login-with-2fa\"", content);
        Assert.Contains("TwoFactorAuthenticatorSignInAsync", content);
    }

    // Requirements 1.1, 5.4 — LoginWithRecoveryCode.razor declares @layout AuthLayout,
    // preserves its FormName literal, and still calls TwoFactorRecoveryCodeSignInAsync
    [Fact]
    public void LoginWithRecoveryCode_PreservesIdentityBehavior()
    {
        var content = File.ReadAllText(LoginWithRecoveryCodePath);

        Assert.Contains("@layout AuthLayout", content);
        Assert.Contains("FormName=\"login-with-recovery-code\"", content);
        Assert.Contains("TwoFactorRecoveryCodeSignInAsync", content);
    }

    // Requirements 1.1, 4.2, 5.5 — VerifyPhone.razor declares @layout AuthLayout,
    // still calls VerifyPhoneOtpAsync/GeneratePhoneOtpAsync, and the Requirement 4.2
    // heading-level fix (<h3> -> <h1>) has not regressed.
    [Fact]
    public void VerifyPhone_PreservesIdentityBehavior()
    {
        var content = File.ReadAllText(VerifyPhonePath);

        Assert.Contains("@layout AuthLayout", content);
        Assert.Contains("VerifyPhoneOtpAsync", content);
        Assert.Contains("GeneratePhoneOtpAsync", content);

        Assert.Equal(1, CountOccurrences(content, "<h1"));
        Assert.Equal(0, CountOccurrences(content, "<h3"));
    }

    // Requirements 1.1, 4.1, 5.6 — ExternalLogin.razor declares @layout AuthLayout,
    // preserves its FormName literal, still calls ExternalLoginSignInAsync/
    // GetExternalLoginInfoAsync, and the Requirement 4.1 single-h1 rule has not regressed.
    [Fact]
    public void ExternalLogin_PreservesIdentityBehavior()
    {
        var content = File.ReadAllText(ExternalLoginPath);

        Assert.Contains("@layout AuthLayout", content);
        Assert.Contains("FormName=\"confirmation\"", content);
        Assert.Contains("ExternalLoginSignInAsync", content);
        Assert.Contains("GetExternalLoginInfoAsync", content);

        Assert.Equal(1, CountOccurrences(content, "<h1"));
        Assert.Equal(0, CountOccurrences(content, "<h2"));
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = haystack.IndexOf(needle, index, System.StringComparison.Ordinal)) != -1)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }
}
