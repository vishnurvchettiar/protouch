using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using ClinicApp.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Identity;

namespace ClinicApp.Components.Account;

internal sealed class SmtpEmailSender : IEmailSender<ApplicationUser>
{
    private readonly IConfiguration _config;

    public SmtpEmailSender(IConfiguration config)
    {
        _config = config;
    }

    private SmtpClient BuildClient()
    {
        var host = _config["Smtp:Host"];
        var port = int.TryParse(_config["Smtp:Port"], out var p) ? p : 25;
        var user = _config["Smtp:Username"];
        var pass = _config["Smtp:Password"];
        var enableSsl = bool.TryParse(_config["Smtp:EnableSsl"], out var ssl) ? ssl : true;

        var client = new SmtpClient(host, port)
        {
            EnableSsl = enableSsl
        };
        if (!string.IsNullOrEmpty(user)) client.Credentials = new NetworkCredential(user, pass);
        return client;
    }

    private string GetFromAddress()
    {
        return _config["Smtp:From"] ?? "no-reply@protouchclinic.local";
    }

    public async Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
    {
        var from = GetFromAddress();
        var subject = "Confirm your ProTouch account";
        var body = $"Please confirm your account by clicking the following link: <a href=\"{confirmationLink}\">Confirm email</a>";
        using var message = new MailMessage(from, email, subject, body) { IsBodyHtml = true };
        using var client = BuildClient();
        await client.SendMailAsync(message);
    }

    public async Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        var from = GetFromAddress();
        var subject = "Reset your ProTouch password";
        var body = $"Reset your password by clicking: <a href=\"{resetLink}\">Reset password</a>";
        using var message = new MailMessage(from, email, subject, body) { IsBodyHtml = true };
        using var client = BuildClient();
        await client.SendMailAsync(message);
    }

    public async Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
    {
        var from = GetFromAddress();
        var subject = "Your ProTouch password reset code";
        var body = $"Use the following code to reset your password: {resetCode}";
        using var message = new MailMessage(from, email, subject, body) { IsBodyHtml = false };
        using var client = BuildClient();
        await client.SendMailAsync(message);
    }
}
