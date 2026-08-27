using System.IO;
using Xunit;

namespace ClinicApp.Tests;

/// <summary>
/// Structural correctness-property test verifying that every in-scope
/// Anonymous_Account_Page renders exactly one top-level "<h1" heading and
/// zero "<h2"/"<h3" occurrences used as a page-level heading substitute.
///
/// This specifically guards InvalidUser.razor and VerifyPhone.razor against
/// regressing back to a "<h3>" heading, and ExternalLogin.razor against
/// regressing back to a second "<h1>"/"<h2>".
///
/// Feature: account-status-pages-consistency, Property 7: Single H1 Per Page
/// Validates: Requirements 4.1, 4.2
/// </summary>
public class Property7SingleH1Tests
{
    private static readonly string SolutionRoot =
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));

    private static readonly string PagesRoot =
        Path.Combine(SolutionRoot, "ClinicApp/Components/Account/Pages");

    public static TheoryData<string> InScopePageRelativePaths => new()
    {
        "Login.razor",
        "Register.razor",
        "ConfirmEmail.razor",
        "ConfirmEmailChange.razor",
        "RegisterConfirmation.razor",
        "ForgotPassword.razor",
        "ForgotPasswordConfirmation.razor",
        "ResetPassword.razor",
        "ResetPasswordConfirmation.razor",
        "InvalidPasswordReset.razor",
        "InvalidUser.razor",
        "Lockout.razor",
        "AccessDenied.razor",
        "LoginWith2fa.razor",
        "LoginWithRecoveryCode.razor",
        "VerifyPhone.razor",
        "ResendEmailConfirmation.razor",
        "ExternalLogin.razor",
    };

    [Theory]
    [MemberData(nameof(InScopePageRelativePaths))]
    public void Page_ContainsExactlyOneH1_AndZeroH2OrH3(string relativePath)
    {
        var fullPath = Path.Combine(PagesRoot, relativePath);
        var content = File.ReadAllText(fullPath);

        // Decorative, aria-hidden="true" blocks (e.g. Login.razor's ambient visual
        // panel) are invisible to assistive tech and are not page-level heading
        // substitutes, so their contents are excluded before counting headings.
        var visibleContent = StripAriaHiddenContent(content);

        var h1Count = CountOccurrences(visibleContent, "<h1");
        var h2Count = CountOccurrences(visibleContent, "<h2");
        var h3Count = CountOccurrences(visibleContent, "<h3");

        Assert.Equal(1, h1Count);
        Assert.Equal(0, h2Count);
        Assert.Equal(0, h3Count);
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

    /// <summary>
    /// Removes the entire contents (opening tag through matching closing tag) of
    /// every "&lt;div&gt;" element whose OWN opening tag carries
    /// <c>aria-hidden="true"</c> (e.g. Login.razor's decorative ambient visual
    /// panel). Elements such as `&lt;span ... aria-hidden="true"&gt;&lt;/span&gt;`
    /// that are not "&lt;div"-based are intentionally left alone — only their own
    /// (empty) contents would be affected, and they are not heading containers.
    ///
    /// This scans "&lt;div" occurrences one at a time, inspects each opening tag's
    /// own attribute text for the aria-hidden marker, and — only when found —
    /// performs a depth-counting scan over subsequent "&lt;div"/"&lt;/div&gt;"
    /// occurrences to locate the matching closing tag, so decorative/hidden
    /// markup is excluded from heading-level assertions.
    /// </summary>
    private static string StripAriaHiddenContent(string content)
    {
        const string marker = "aria-hidden=\"true\"";
        var result = content;
        var searchFrom = 0;

        while (true)
        {
            var tagStart = result.IndexOf("<div", searchFrom, System.StringComparison.Ordinal);
            if (tagStart == -1)
            {
                break;
            }

            var openTagEnd = result.IndexOf('>', tagStart);
            if (openTagEnd == -1)
            {
                break;
            }

            var openingTagText = result.Substring(tagStart, openTagEnd - tagStart);
            if (!openingTagText.Contains(marker, System.StringComparison.Ordinal))
            {
                // This <div> isn't aria-hidden itself — keep scanning after it.
                searchFrom = openTagEnd + 1;
                continue;
            }

            // Depth-count "<div" vs "</div>" occurrences from just after the opening
            // tag to find the matching closing tag for this div.
            var depth = 1;
            var scanPos = openTagEnd + 1;
            var closeTagEnd = -1;

            while (scanPos < result.Length)
            {
                var nextOpen = result.IndexOf("<div", scanPos, System.StringComparison.Ordinal);
                var nextClose = result.IndexOf("</div>", scanPos, System.StringComparison.Ordinal);

                if (nextClose == -1)
                {
                    // Malformed markup — nothing more we can safely strip; bail out.
                    break;
                }

                if (nextOpen != -1 && nextOpen < nextClose)
                {
                    depth++;
                    scanPos = nextOpen + 4;
                }
                else
                {
                    depth--;
                    scanPos = nextClose + "</div>".Length;
                    if (depth == 0)
                    {
                        closeTagEnd = scanPos;
                        break;
                    }
                }
            }

            if (closeTagEnd == -1)
            {
                // Could not find a balanced closing tag; bail out to avoid an infinite loop.
                break;
            }

            result = result.Remove(tagStart, closeTagEnd - tagStart);
            searchFrom = tagStart;
        }

        return result;
    }
}
