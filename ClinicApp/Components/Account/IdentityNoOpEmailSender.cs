using Microsoft.AspNetCore.Identity;
using ClinicApp.Data;

namespace ClinicApp.Components.Account;

internal sealed class IdentityNoOpEmailSender : IEmailSender<ApplicationUser>
{
    // Used when SMTP is not configured. Registration confirmation generates
    // a confirmation link locally, so these methods intentionally do nothing.
    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink) =>
        Task.CompletedTask;

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink) =>
        Task.CompletedTask;

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode) =>
        Task.CompletedTask;
}
