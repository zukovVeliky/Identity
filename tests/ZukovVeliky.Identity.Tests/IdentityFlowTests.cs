using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace ZukovVeliky.Identity.Tests;

public sealed partial class IdentityFlowTests : IDisposable
{
    private const string AdminEmail = "admin@example.test";
    private const string EditorEmail = "editor@example.test";
    private const string Password = "Zkusebni-Heslo-1";

    private readonly string _folder = Directory.CreateTempSubdirectory("identity-kit-").FullName;
    private readonly CapturingEmailSender _mail = new();

    private WebApplicationFactory<Program> Site(params (string Key, string? Value)[] settings)
    {
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(host =>
        {
            host.UseEnvironment("Testing");
            host.UseSetting("ConnectionStrings:DatabaseIdentity", $"Data Source={Path.Combine(_folder, "identity.db")};Pooling=False");
            foreach (var (key, value) in settings)
            {
                host.UseSetting(key, value);
            }

            host.ConfigureTestServices(services =>
            {
                services.RemoveAll<IEmailSender>();
                services.AddSingleton<IEmailSender>(_mail);
            });
        });
        return factory;
    }

    private WebApplicationFactory<Program> SeededSite(params (string Key, string? Value)[] settings)
        => Site([
            ("IdentityBootstrap:AdminEmail", AdminEmail),
            ("IdentityBootstrap:AdminPassword", Password),
            ("IdentityBootstrap:EditorEmail", EditorEmail),
            ("IdentityBootstrap:EditorPassword", Password),
            .. settings
        ]);

    private static HttpClient Client(WebApplicationFactory<Program> site)
        => site.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, BaseAddress = new Uri("https://localhost") });

    private static async Task<HttpResponseMessage> PostFormAsync(HttpClient client, string page, string? handler, params (string Key, string Value)[] fields)
    {
        var html = await (await client.GetAsync(page)).Content.ReadAsStringAsync();
        var token = TokenRegex().Match(html);
        Assert.True(token.Success, $"No antiforgery token on {page}");
        var values = fields.ToDictionary(x => x.Key, x => x.Value);
        values["__RequestVerificationToken"] = token.Groups[1].Value;
        var target = handler is null ? page : $"{page}{(page.Contains('?') ? '&' : '?')}handler={handler}";
        return await client.PostAsync(target, new FormUrlEncodedContent(values));
    }

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password = Password)
        => PostFormAsync(client, "/Identity/Account/Login", null, ("Input.Email", email), ("Input.Password", password));

    private static Task<HttpResponseMessage> RegisterAsync(HttpClient client, string email)
        => PostFormAsync(client, "/Identity/Account/Register", null, ("Input.Email", email), ("Input.Password", Password), ("Input.ConfirmPassword", Password));

    [Fact]
    public async Task Czech_pages_replace_the_default_identity_ui()
    {
        using var site = Site();
        var html = await (await Client(site).GetAsync("/Identity/Account/Login")).Content.ReadAsStringAsync();
        Assert.Contains("Přihlaste se pomocí místního účtu", html);
    }

    [Fact]
    public async Task Short_login_address_works()
    {
        using var site = Site();
        Assert.Equal(HttpStatusCode.OK, (await Client(site).GetAsync("/Login")).StatusCode);
    }

    [Fact]
    public async Task New_account_signs_in_only_after_confirming_the_email()
    {
        using var site = Site(("IdentityKit:SiteName", "Můj web"));
        var client = Client(site);

        Assert.Equal(HttpStatusCode.Redirect, (await RegisterAsync(client, "novy@example.test")).StatusCode);
        var mail = Assert.Single(_mail.Sent);
        Assert.Contains("Můj web", mail.Subject);

        var refused = await LoginAsync(client, "novy@example.test");
        Assert.Equal(HttpStatusCode.OK, refused.StatusCode);

        var link = WebUtility.HtmlDecode(LinkRegex().Match(mail.Body).Groups[1].Value);
        Assert.Contains("/Identity/Account/ConfirmEmail", link);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(link)).StatusCode);

        var signedIn = await LoginAsync(client, "novy@example.test");
        Assert.Equal(HttpStatusCode.Redirect, signedIn.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/Identity/Account/Manage")).StatusCode);
    }

    [Fact]
    public async Task Resent_confirmation_link_works()
    {
        using var site = Site();
        var client = Client(site);
        await RegisterAsync(client, "znovu@example.test");

        await PostFormAsync(client, "/Identity/Account/ResendEmailConfirmation", null, ("Input.Email", "znovu@example.test"));
        Assert.Equal(2, _mail.Sent.Count);

        var link = WebUtility.HtmlDecode(LinkRegex().Match(_mail.Sent[1].Body).Groups[1].Value);
        Assert.Contains("/Identity/Account/ConfirmEmail", link);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync(link)).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await LoginAsync(client, "znovu@example.test")).StatusCode);
    }

    [Fact]
    public async Task Registration_can_be_turned_off()
    {
        using var site = Site(("IdentityKit:AllowPublicRegistration", "false"));
        Assert.Equal(HttpStatusCode.NotFound, (await Client(site).GetAsync("/Identity/Account/Register")).StatusCode);
    }

    [Fact]
    public async Task Without_settings_no_account_is_created_but_roles_exist()
    {
        using var site = Site();
        using var scope = site.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        Assert.Empty(users.Users);
        Assert.True(await roles.RoleExistsAsync(Roles.Administrator));
        Assert.True(await roles.RoleExistsAsync(Roles.Editor));
    }

    [Fact]
    public async Task Bootstrap_is_repeatable()
    {
        using (SeededSite().Services.CreateScope())
        {
        }

        using var site = SeededSite();
        using var scope = site.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        Assert.Equal(2, users.Users.Count());
    }

    [Fact]
    public async Task Role_pages_require_an_administrator()
    {
        using var site = SeededSite();

        var anonymous = await Client(site).GetAsync("/Identity/Account/RolesUsers/SeznamUsers");
        Assert.Equal(HttpStatusCode.Redirect, anonymous.StatusCode);
        Assert.Contains("/Identity/Account/Login", anonymous.Headers.Location!.ToString());

        var editor = Client(site);
        await LoginAsync(editor, EditorEmail);
        var denied = await editor.GetAsync("/Identity/Account/RolesUsers/SeznamUsers");
        Assert.Equal(HttpStatusCode.Redirect, denied.StatusCode);
        Assert.Contains("AccessDenied", denied.Headers.Location!.ToString());

        var admin = Client(site);
        await LoginAsync(admin, AdminEmail);
        var list = await (await admin.GetAsync("/Identity/Account/RolesUsers/SeznamUsers")).Content.ReadAsStringAsync();
        Assert.Contains(EditorEmail, list);
    }

    [Fact]
    public async Task Administrator_grants_and_removes_a_role_and_role_counts_as_confirmation()
    {
        using var site = SeededSite();
        var visitor = Client(site);
        await RegisterAsync(visitor, "host@example.test");
        Assert.Equal(HttpStatusCode.OK, (await LoginAsync(visitor, "host@example.test")).StatusCode);

        string userPage, roleId;
        using (var scope = site.Services.CreateScope())
        {
            var user = await scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>().FindByEmailAsync("host@example.test");
            var role = await scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>().FindByNameAsync(Roles.Editor);
            userPage = $"/Identity/Account/RolesUsers/UserEditRole?cip={Uri.EscapeDataString(CodeDecode.Base64Encode(user!.Id))}";
            roleId = CodeDecode.Base64Encode(role!.Id);
        }

        var admin = Client(site);
        await LoginAsync(admin, AdminEmail);
        var cip = Uri.UnescapeDataString(userPage[(userPage.IndexOf("cip=") + 4)..]);

        await PostFormAsync(admin, userPage, "Add", ("id", roleId), ("cip", cip));
        Assert.Equal(HttpStatusCode.Redirect, (await LoginAsync(Client(site), "host@example.test")).StatusCode);

        await PostFormAsync(admin, userPage, "Remove", ("id", roleId), ("cip", cip));
        using var check = site.Services.CreateScope();
        var manager = check.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        Assert.False(await manager.IsInRoleAsync((await manager.FindByEmailAsync("host@example.test"))!, Roles.Editor));
    }

    [Fact]
    public async Task Last_administrator_cannot_be_removed()
    {
        using var site = SeededSite();
        string userPage, roleId, cip;
        using (var scope = site.Services.CreateScope())
        {
            var user = await scope.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>().FindByEmailAsync(AdminEmail);
            var role = await scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>().FindByNameAsync(Roles.Administrator);
            cip = CodeDecode.Base64Encode(user!.Id);
            userPage = $"/Identity/Account/RolesUsers/UserEditRole?cip={Uri.EscapeDataString(cip)}";
            roleId = CodeDecode.Base64Encode(role!.Id);
        }

        var admin = Client(site);
        await LoginAsync(admin, AdminEmail);
        await PostFormAsync(admin, userPage, "Remove", ("id", roleId), ("cip", cip));

        using var check = site.Services.CreateScope();
        var manager = check.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();
        Assert.True(await manager.IsInRoleAsync((await manager.FindByEmailAsync(AdminEmail))!, Roles.Administrator));
    }

    [Fact]
    public async Task Built_in_role_cannot_be_deleted_and_a_custom_one_can()
    {
        using var site = SeededSite();
        var admin = Client(site);
        await LoginAsync(admin, AdminEmail);
        const string page = "/Identity/Account/RolesUsers/SeznamEditRole";

        await PostFormAsync(admin, page, "CreateRole", ("textRole", "Pokladna"));

        string builtInId, customId;
        using (var scope = site.Services.CreateScope())
        {
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            builtInId = (await roles.FindByNameAsync(Roles.Administrator))!.Id;
            customId = (await roles.FindByNameAsync("Pokladna"))!.Id;
        }

        await PostFormAsync(admin, page, "Delete", ("id", builtInId));
        await PostFormAsync(admin, page, "Delete", ("id", customId));

        using var check = site.Services.CreateScope();
        var manager = check.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        Assert.True(await manager.RoleExistsAsync(Roles.Administrator));
        Assert.False(await manager.RoleExistsAsync("Pokladna"));
    }

    [Fact]
    public async Task Repeated_wrong_passwords_lock_the_account()
    {
        using var site = SeededSite();
        var client = Client(site);
        for (var i = 0; i < 5; i++)
        {
            await LoginAsync(client, EditorEmail, "spatne-heslo");
        }

        var locked = await LoginAsync(client, EditorEmail);
        Assert.Equal(HttpStatusCode.Redirect, locked.StatusCode);
        Assert.Contains("Lockout", locked.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Forgotten_password_can_be_reset()
    {
        using var site = SeededSite();
        var client = Client(site);
        await PostFormAsync(client, "/Identity/Account/ForgotPassword", null, ("Input.Email", EditorEmail));

        var link = WebUtility.HtmlDecode(LinkRegex().Match(Assert.Single(_mail.Sent).Body).Groups[1].Value);
        var html = await (await client.GetAsync(link)).Content.ReadAsStringAsync();
        var code = WebUtility.HtmlDecode(Regex.Match(html, "name=\"Input.Code\"[^>]*value=\"([^\"]+)\"|value=\"([^\"]+)\"[^>]*name=\"Input.Code\"").Groups.Values.Skip(1).First(g => g.Success).Value);
        const string newPassword = "Nove-Heslo-2";
        var reset = await PostFormAsync(client, link, null,
            ("Input.Email", EditorEmail), ("Input.Password", newPassword), ("Input.ConfirmPassword", newPassword), ("Input.Code", code));
        Assert.Equal(HttpStatusCode.Redirect, reset.StatusCode);

        Assert.Equal(HttpStatusCode.Redirect, (await LoginAsync(Client(site), EditorEmail, newPassword)).StatusCode);
    }

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        try
        {
            Directory.Delete(_folder, true);
        }
        catch (IOException)
        {
            // The system cleans its temporary folder later.
        }
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"")]
    private static partial Regex TokenRegex();

    [GeneratedRegex("href=['\"]([^'\"]+)['\"]")]
    private static partial Regex LinkRegex();

    private sealed class CapturingEmailSender : IEmailSender
    {
        public List<(string To, string Subject, string Body)> Sent { get; } = [];

        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            Sent.Add((email, subject, htmlMessage));
            return Task.CompletedTask;
        }
    }
}
