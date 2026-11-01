using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ZukovVeliky.Identity.Tests;

public sealed class SchemaAndHelpersTests
{
    private static string RepositoryFile(params string[] parts)
    {
        var folder = new DirectoryInfo(AppContext.BaseDirectory);
        while (folder is not null && !File.Exists(Path.Combine(folder.FullName, "Identity.sln")))
        {
            folder = folder.Parent;
        }

        Assert.NotNull(folder);
        return Path.Combine([folder!.FullName, .. parts]);
    }

    private static string Normalize(string text) => text.Replace("\r\n", "\n").Trim();

    /// <summary>Set UPDATE_SCHEMA_SCRIPT=1 to rewrite sql/identity-schema-sqlserver.sql after a model change.</summary>
    [Fact]
    public void Sql_script_matches_the_model()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer("Server=none").Options;
        using var db = new ApplicationDbContext(options);
        // Filtered indexes need this setting and sqlcmd has it off by default.
        var script = "SET QUOTED_IDENTIFIER ON;\nGO\n\n" + db.Database.GenerateCreateScript();
        var path = RepositoryFile("sql", "identity-schema-sqlserver.sql");

        if (Environment.GetEnvironmentVariable("UPDATE_SCHEMA_SCRIPT") == "1")
        {
            File.WriteAllText(path, Normalize(script) + "\n");
        }

        Assert.Equal(Normalize(script), Normalize(File.ReadAllText(path)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("0193a1b2-7c3d-Žluťoučký")]
    public void Encoding_round_trips(string value)
        => Assert.Equal(value, CodeDecode.Base64Decode(CodeDecode.Base64Encode(value)));

    [Fact]
    public void Bootstrap_has_no_built_in_password()
    {
        var options = new IdentityBootstrapOptions();
        Assert.Null(options.AdminEmail);
        Assert.Null(options.AdminPassword);
        Assert.Null(options.EditorEmail);
        Assert.Null(options.EditorPassword);
    }
}
