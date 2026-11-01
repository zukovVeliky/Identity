using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace ZukovVeliky.Identity;

/// <summary>Settings of the section "IdentityKit".</summary>
public sealed class IdentityKitOptions
{
    public const string SectionName = "IdentityKit";

    /// <summary>Name shown in e-mails and in authenticator apps.</summary>
    public string SiteName { get; set; } = "Web";

    /// <summary>When false, the registration page answers 404.</summary>
    public bool AllowPublicRegistration { get; set; } = true;

    /// <summary>Short address of the login page; empty keeps only /Identity/Account/Login.</summary>
    public string? LoginRoute { get; set; } = "/Login";
}

public static class AuthorizationPolicies
{
    public const string AdminOnly = "AdminOnly";
    public const string EditorOrAdmin = "EditorOrAdmin";
}

public static class IdentityKitExtensions
{
    public const string ConnectionStringName = "DatabaseIdentity";

    /// <summary>
    /// Adds accounts, roles, the Czech pages under /Identity and the policies AdminOnly and EditorOrAdmin.
    /// Without <paramref name="database"/> it uses SQL Server with the connection string DatabaseIdentity.
    /// An IEmailSender registered before this call replaces the SMTP sender.
    /// </summary>
    public static IdentityBuilder AddIdentityKit(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<DbContextOptionsBuilder>? database = null)
    {
        var kit = configuration.GetSection(IdentityKitOptions.SectionName).Get<IdentityKitOptions>() ?? new IdentityKitOptions();

        services.Configure<IdentityKitOptions>(configuration.GetSection(IdentityKitOptions.SectionName));
        services.Configure<IdentityBootstrapOptions>(configuration.GetSection(IdentityBootstrapOptions.SectionName));
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            if (database is not null)
            {
                database(options);
                return;
            }

            var connection = configuration.GetConnectionString(ConnectionStringName)
                ?? throw new InvalidOperationException($"Connection string '{ConnectionStringName}' was not found.");
            options.UseSqlServer(connection);
        });

        // Goes first: the default UI registers its own sender, which sends nothing, only when none exists yet.
        services.TryAddSingleton<IEmailSender, EmailSender>();

        var builder = services.AddDefaultIdentity<IdentityUser>(options =>
            {
                // New accounts confirm their e-mail first; accounts with a role are treated as confirmed.
                options.SignIn.RequireConfirmedAccount = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddUserConfirmation<RoleOrEmailUserConfirmation>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicies.AdminOnly, policy => policy.RequireRole(Roles.Administrator));
            options.AddPolicy(AuthorizationPolicies.EditorOrAdmin, policy => policy.RequireRole(Roles.Editor, Roles.Administrator));
        });

        services.AddRazorPages();
        services.Configure<RazorPagesOptions>(options =>
        {
            options.Conventions.AuthorizeAreaFolder("Identity", "/Account/RolesUsers", AuthorizationPolicies.AdminOnly);
            if (!string.IsNullOrWhiteSpace(kit.LoginRoute))
            {
                options.Conventions.AddAreaPageRoute("Identity", "/Account/Login", kit.LoginRoute);
            }
        });

        return builder;
    }

    /// <summary>Creates the built-in roles and the accounts from IdentityBootstrap. Run it after the tables exist.</summary>
    public static async Task InitializeIdentityKitAsync(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        await IdentitySeed.EnsureInitializedAsync(scope.ServiceProvider);
    }
}
