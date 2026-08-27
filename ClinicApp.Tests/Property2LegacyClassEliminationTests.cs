using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace ClinicApp.Tests;

// ============================================================
// Property 2: Legacy Class Family Elimination
// Validates: Requirements 2.2, 2.3, 2.4
//
// For any page in the Anonymous_Account_Page set (all 18 in-scope
// pages), its source SHALL NOT contain any CSS class token beginning
// with "login-" or "auth-" (the retired legacy families), other than
// the canonical "pt-auth-" prefix.
// ============================================================
public class Property2LegacyClassEliminationTests
{
    private static readonly string SolutionRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));

    private static readonly string PagesRoot =
        Path.Combine(SolutionRoot, "ClinicApp/Components/Account/Pages");

    // Matches a class="..." attribute value so we can inspect individual class tokens
    // rather than doing a raw substring search (which would false-positive against
    // the canonical "pt-auth-" prefix, since "auth-" is a substring of "pt-auth-").
    private static readonly Regex ClassAttributeRegex = new(
        "class=\"([^\"]*)\"",
        RegexOptions.Compiled);

    // Anchored to the start of each individual class token — "pt-auth-foo" does not
    // match this pattern because the match is against the whole token, not a substring.
    private static readonly Regex LegacyClassTokenRegex = new(
        "^(login-|auth-)",
        RegexOptions.Compiled);

    public static IEnumerable<object[]> InScopePages()
    {
        yield return new object[] { "Login.razor" };
        yield return new object[] { "Register.razor" };
        yield return new object[] { "ConfirmEmail.razor" };
        yield return new object[] { "ConfirmEmailChange.razor" };
        yield return new object[] { "RegisterConfirmation.razor" };
        yield return new object[] { "ForgotPassword.razor" };
        yield return new object[] { "ForgotPasswordConfirmation.razor" };
        yield return new object[] { "ResetPassword.razor" };
        yield return new object[] { "ResetPasswordConfirmation.razor" };
        yield return new object[] { "InvalidPasswordReset.razor" };
        yield return new object[] { "InvalidUser.razor" };
        yield return new object[] { "Lockout.razor" };
        yield return new object[] { "AccessDenied.razor" };
        yield return new object[] { "LoginWith2fa.razor" };
        yield return new object[] { "LoginWithRecoveryCode.razor" };
        yield return new object[] { "VerifyPhone.razor" };
        yield return new object[] { "ResendEmailConfirmation.razor" };
        yield return new object[] { "ExternalLogin.razor" };
    }

    [Theory]
    [MemberData(nameof(InScopePages))]
    public void Page_ContainsNoLegacyClassToken(string fileName)
    {
        var path = Path.Combine(PagesRoot, fileName);
        var content = File.ReadAllText(path);

        var offendingTokens = new List<string>();

        foreach (Match classAttrMatch in ClassAttributeRegex.Matches(content))
        {
            var classValue = classAttrMatch.Groups[1].Value;
            var tokens = classValue.Split(
                (char[]?)null,
                System.StringSplitOptions.RemoveEmptyEntries);

            foreach (var token in tokens)
            {
                if (LegacyClassTokenRegex.IsMatch(token))
                {
                    offendingTokens.Add(token);
                }
            }
        }

        Assert.True(
            offendingTokens.Count == 0,
            $"{fileName} contains legacy class token(s): {string.Join(", ", offendingTokens)}");
    }
}
