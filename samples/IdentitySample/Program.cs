using IdentitySample;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using ZukovVeliky.Identity;

var builder = WebApplication.CreateBuilder(args);

// The sample writes e-mails to the console instead of sending them. Remove this line to use SMTP from EmailSettings.
builder.Services.AddSingleton<IEmailSender, ConsoleEmailSender>();

// SQLite keeps the sample free of a database server. Sample:UseSqlite=false switches to SQL Server, the solution's default,
// where the tables come from sql/identity-schema-sqlserver.sql.
var useSqlite = builder.Configuration.GetValue("Sample:UseSqlite", true);
if (useSqlite)
{
    builder.Services.AddIdentityKit(builder.Configuration, db =>
        db.UseSqlite(builder.Configuration.GetConnectionString(IdentityKitExtensions.ConnectionStringName)));
}
else
{
    builder.Services.AddIdentityKit(builder.Configuration);
}

var app = builder.Build();

if (useSqlite)
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreatedAsync();
}

await app.Services.InitializeIdentityKitAsync();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();

app.Run();

public partial class Program;