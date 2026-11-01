using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MimeKit;

namespace ZukovVeliky.Identity;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<IdentityUser>(options);

/// <summary>Roles the solution relies on. Other roles can be added on the administration page.</summary>
public static class Roles
{
    public const string Administrator = "Administrator";
    public const string Editor = "Editor";

    public static readonly string[] BuiltIn = [Administrator, Editor];
}

/// <summary>
/// A new account may sign in only after confirming its e-mail. Accounts that already have a role
/// (editors, administrators) count as confirmed, so an administrator can let a person in.
/// </summary>
public class RoleOrEmailUserConfirmation : IUserConfirmation<IdentityUser>
{
    public async Task<bool> IsConfirmedAsync(UserManager<IdentityUser> manager, IdentityUser user)
    {
        if (await manager.IsEmailConfirmedAsync(user))
        {
            return true;
        }

        var roles = await manager.GetRolesAsync(user);
        return roles.Count > 0;
    }
}

/// <summary>Accounts created by InitializeIdentityKitAsync. There are no defaults, so nothing is created without settings.</summary>
public sealed class IdentityBootstrapOptions
{
    public const string SectionName = "IdentityBootstrap";

    public string? AdminEmail { get; set; }
    public string? AdminPassword { get; set; }
    public string? EditorEmail { get; set; }
    public string? EditorPassword { get; set; }
}

public static class IdentitySeed
{
    public static async Task EnsureInitializedAsync(IServiceProvider services)
    {
        var options = services.GetRequiredService<IOptions<IdentityBootstrapOptions>>().Value;
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<IdentityUser>>();

        foreach (var role in Roles.BuiltIn)
        {
            await EnsureRoleAsync(roleManager, role);
        }

        await EnsureUserAsync(userManager, options.AdminEmail, options.AdminPassword, Roles.Administrator);
        await EnsureUserAsync(userManager, options.EditorEmail, options.EditorPassword, Roles.Editor);
    }

    private static async Task EnsureRoleAsync(RoleManager<IdentityRole> roleManager, string role)
    {
        if (await roleManager.RoleExistsAsync(role))
        {
            return;
        }

        Check(await roleManager.CreateAsync(new IdentityRole(role)), $"create role '{role}'");
    }

    private static async Task EnsureUserAsync(UserManager<IdentityUser> userManager, string? email, string? password, string role)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            if (string.IsNullOrEmpty(password))
            {
                return;
            }

            user = new IdentityUser { UserName = email, Email = email, EmailConfirmed = true };
            Check(await userManager.CreateAsync(user, password), $"create user '{email}'");
        }

        if (!await userManager.IsInRoleAsync(user, role))
        {
            Check(await userManager.AddToRoleAsync(user, role), $"add role '{role}' to '{email}'");
        }
    }

    private static void Check(IdentityResult result, string action)
    {
        if (!result.Succeeded)
        {
            throw new InvalidOperationException($"Unable to {action}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }
    }
}

public class EmailSettings
{
    public const string SectionName = "EmailSettings";

    public string MailServer { get; set; } = string.Empty;
    public int MailPort { get; set; } = 465;
    public bool UseSsl { get; set; } = true;
    public string SenderName { get; set; } = string.Empty;
    public string Sender { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

/// <summary>Sends the confirmation and password reset e-mails over SMTP.</summary>
public class EmailSender(IOptions<EmailSettings> settings, ILogger<EmailSender> logger) : IEmailSender
{
    private readonly EmailSettings _settings = settings.Value;

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.SenderName, _settings.Sender));
            message.To.Add(MailboxAddress.Parse(email));
            message.Subject = subject;
            message.Body = new TextPart("html") { Text = htmlMessage };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_settings.MailServer, _settings.MailPort, _settings.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable);
            if (!string.IsNullOrEmpty(_settings.User))
            {
                await smtp.AuthenticateAsync(_settings.User, _settings.Password);
            }

            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "E-mail could not be sent. Subject={Subject}", subject);
            throw;
        }
    }
}

/// <summary>Reversible encoding of ids in role page addresses. It is not protection: access is decided by the AdminOnly policy.</summary>
public static class CodeDecode
{
    public static string Base64Encode(string plainText)
        => Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(new string(plainText.Reverse().ToArray())));

    public static string Base64Decode(string encoded)
        => new(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(encoded)).Reverse().ToArray());
}
