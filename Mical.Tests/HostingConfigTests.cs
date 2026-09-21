using Mical.Helpers;
using Microsoft.Extensions.Configuration;

namespace Mical.Tests;

/// <summary>
/// Pure configuration logic: no database, no container. These are the pieces that
/// decide whether a deploy comes up at all, so they are worth pinning down.
/// </summary>
public class HostingConfigTests
{
    private static IConfiguration Config(params (string Key, string? Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();

    // ----- DATABASE_URL -> Npgsql -----

    [Fact]
    public void A_database_url_becomes_an_npgsql_connection_string()
    {
        var result = HostingConfig.NpgsqlFromDatabaseUrl(
            "postgresql://mical:s3cret@monorail.proxy.rlwy.net:41234/railway");

        Assert.NotNull(result);
        Assert.Contains("Host=monorail.proxy.rlwy.net", result);
        Assert.Contains("Port=41234", result);
        Assert.Contains("Database=railway", result);
        Assert.Contains("Username=mical", result);
        Assert.Contains("Password=s3cret", result);
    }

    /// <summary>
    /// Prefer, not Require: it encrypts against a managed database that offers TLS
    /// and still connects to a plain local Postgres. Hardcoding Require made the
    /// container die on startup against a database without SSL.
    /// </summary>
    [Fact]
    public void TLS_is_negotiated_rather_than_demanded_by_default()
    {
        var result = HostingConfig.NpgsqlFromDatabaseUrl("postgres://u:p@db.internal:5432/mical");

        Assert.NotNull(result);
        Assert.Contains("SSL Mode=Prefer", result);
    }

    [Theory]
    [InlineData("require", "SSL Mode=Require")]
    [InlineData("disable", "SSL Mode=Disable")]
    [InlineData("verify-full", "SSL Mode=VerifyFull")]
    public void An_explicit_sslmode_in_the_url_wins(string sslmode, string expected)
    {
        var result = HostingConfig.NpgsqlFromDatabaseUrl($"postgres://u:p@db.internal:5432/mical?sslmode={sslmode}");

        Assert.NotNull(result);
        Assert.Contains(expected, result);
    }

    [Fact]
    public void An_unrecognized_sslmode_falls_back_instead_of_crashing()
    {
        var result = HostingConfig.NpgsqlFromDatabaseUrl("postgres://u:p@db.internal:5432/mical?sslmode=cualquiera");

        Assert.NotNull(result);
        Assert.Contains("SSL Mode=Prefer", result);
    }

    [Fact]
    public void The_shorter_postgres_scheme_is_accepted_too()
    {
        var result = HostingConfig.NpgsqlFromDatabaseUrl("postgres://u:p@db.internal:5432/mical");

        Assert.NotNull(result);
        Assert.Contains("Database=mical", result);
    }

    [Fact]
    public void A_url_without_a_port_falls_back_to_the_postgres_default()
    {
        var result = HostingConfig.NpgsqlFromDatabaseUrl("postgres://u:p@db.internal/mical");

        Assert.NotNull(result);
        Assert.Contains("Port=5432", result);
    }

    [Fact]
    public void A_percent_encoded_password_is_decoded()
    {
        var result = HostingConfig.NpgsqlFromDatabaseUrl("postgres://u:p%40ss%3Aword@db.internal:5432/mical");

        Assert.NotNull(result);
        // Npgsql quotes values that contain its own separators.
        Assert.Contains("p@ss:word", result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no soy una url")]
    [InlineData("https://example.com/db")]
    [InlineData("mysql://u:p@host/db")]
    public void Anything_that_is_not_a_postgres_url_is_rejected(string? value)
    {
        Assert.Null(HostingConfig.NpgsqlFromDatabaseUrl(value));
    }

    // ----- Connection string resolution -----

    [Fact]
    public void An_explicit_connection_string_wins_over_the_platform_url()
    {
        var config = Config(
            ("ConnectionStrings:DefaultConnection", "Host=explicito;Database=mical"),
            ("DATABASE_URL", "postgres://u:p@plataforma:5432/otra"));

        Assert.Equal("Host=explicito;Database=mical", HostingConfig.ResolveConnectionString(config));
    }

    [Fact]
    public void Without_an_explicit_one_the_platform_url_is_used()
    {
        var config = Config(("DATABASE_URL", "postgres://u:p@plataforma:5432/railway"));

        Assert.Contains("Host=plataforma", HostingConfig.ResolveConnectionString(config));
    }

    [Fact]
    public void With_no_database_configured_startup_fails_loudly()
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => HostingConfig.ResolveConnectionString(Config()));

        Assert.Contains("cadena de conexión", ex.Message);
    }

    // ----- PORT -----

    [Fact]
    public void The_platform_port_becomes_a_bind_url_on_every_interface()
    {
        Assert.Equal("http://0.0.0.0:8080", HostingConfig.BindUrlFromPortVariable(Config(("PORT", "8080"))));
    }

    [Fact]
    public void An_explicit_ASPNETCORE_URLS_is_never_overridden()
    {
        var config = Config(("ASPNETCORE_URLS", "http://localhost:5000"), ("PORT", "8080"));

        Assert.Null(HostingConfig.BindUrlFromPortVariable(config));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("no-es-un-numero")]
    public void Without_a_usable_port_the_host_decides(string? port)
    {
        Assert.Null(HostingConfig.BindUrlFromPortVariable(Config(("PORT", port))));
    }

    // ----- Uploads root -----

    [Fact]
    public void Uploads_default_to_a_folder_under_wwwroot()
    {
        var webRoot = Path.Combine(Path.GetTempPath(), $"mical-webroot-{Guid.NewGuid():N}");

        try
        {
            var root = HostingConfig.ResolveUploadsRoot(Config(), webRoot);

            Assert.Equal(Path.Combine(webRoot, "uploads"), root);
            Assert.True(Directory.Exists(root));
        }
        finally
        {
            if (Directory.Exists(webRoot)) Directory.Delete(webRoot, recursive: true);
        }
    }

    /// <summary>
    /// A relative path would resolve against the working directory, so the images
    /// would land on the container's ephemeral filesystem while the operator
    /// believes a volume is in use. Fail at startup instead of losing data quietly.
    /// </summary>
    [Theory]
    [InlineData("data/uploads")]
    [InlineData("./uploads")]
    [InlineData("uploads")]
    public void A_relative_uploads_path_is_refused_at_startup(string configured)
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => HostingConfig.ResolveUploadsRoot(Config(("Storage:UploadsPath", configured)), "ignorado"));

        Assert.Contains("absoluta", ex.Message);
    }

    [Fact]
    public void A_configured_path_wins_and_is_created_if_missing()
    {
        var volume = Path.Combine(Path.GetTempPath(), $"mical-volume-{Guid.NewGuid():N}");

        try
        {
            var root = HostingConfig.ResolveUploadsRoot(Config(("Storage:UploadsPath", volume)), "ignorado");

            Assert.Equal(Path.GetFullPath(volume), root);
            Assert.True(Directory.Exists(root));
        }
        finally
        {
            if (Directory.Exists(volume)) Directory.Delete(volume, recursive: true);
        }
    }
}
