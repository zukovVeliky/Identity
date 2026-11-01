using Microsoft.AspNetCore.Identity.UI.Services;

namespace IdentitySample;

/// <summary>For trying the sample only: the confirmation link appears in the console.</summary>
public sealed class ConsoleEmailSender(ILogger<ConsoleEmailSender> logger) : IEmailSender
{
    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        logger.LogInformation("E-mail pro {Email}: {Subject}\n{Body}", email, subject, htmlMessage);
        return Task.CompletedTask;
    }
}