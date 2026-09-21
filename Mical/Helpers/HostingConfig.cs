using Microsoft.AspNetCore.WebUtilities;
using Npgsql;

namespace Mical.Helpers;

/// <summary>
/// Adapts platform-provided environment variables (Railway and similar PaaS) to
/// what the application expects. Kept free of framework types so it can be
/// covered by tests.
/// </summary>
public static class HostingConfig
{
    /// <summary>Physical folder that backs the <c>/uploads</c> URL path.</summary>
    public const string UploadsRequestPath = "/uploads";

    /// <summary>
    /// Turns a <c>postgres://user:pass@host:port/db</c> URL into an Npgsql
    /// connection string. Railway (like Heroku and Render) publishes the database
    /// that way, and Npgsql does not accept the URL form.
    /// Returns <c>null</c> when the value is not a recognizable database URL.
    /// </summary>
    public static string? NpgsqlFromDatabaseUrl(string? databaseUrl)
    {
        if (string.IsNullOrWhiteSpace(databaseUrl))
            return null;

        if (!Uri.TryCreate(databaseUrl, UriKind.Absolute, out var uri))
            return null;

        if (uri.Scheme is not ("postgres" or "postgresql"))
            return null;

        var credentials = uri.UserInfo.Split(':', 2);
        var database = uri.AbsolutePath.TrimStart('/');

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Database = string.IsNullOrEmpty(database) ? "postgres" : database,
            Username = Uri.UnescapeDataString(credentials[0]),
            Password = credentials.Length > 1 ? Uri.UnescapeDataString(credentials[1]) : string.Empty,
            SslMode = SslModeFrom(uri.Query)
        };

        return builder.ConnectionString;
    }

    /// <summary>
    /// TLS mode for a platform URL. Defaults to <see cref="SslMode.Prefer"/>: it
    /// encrypts against a managed database that offers TLS and still connects to a
    /// plain local Postgres, instead of failing on one or the other. An explicit
    /// <c>?sslmode=</c> in the URL wins.
    /// </summary>
    private static SslMode SslModeFrom(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return SslMode.Prefer;

        if (!QueryHelpers.ParseQuery(query).TryGetValue("sslmode", out var values))
            return SslMode.Prefer;

        var requested = values.ToString();
        if (string.IsNullOrWhiteSpace(requested))
            return SslMode.Prefer;

        // libpq spells these with a hyphen (verify-ca), the enum does not.
        var normalized = requested.Replace("-", string.Empty);

        return Enum.TryParse<SslMode>(normalized, ignoreCase: true, out var mode)
            ? mode
            : SslMode.Prefer;
    }

    /// <summary>
    /// Resolves the connection string: the explicit one wins, then a platform
    /// <c>DATABASE_URL</c>. Throws when neither is configured, because starting
    /// without a database would only fail later and less clearly.
    /// </summary>
    public static string ResolveConnectionString(IConfiguration configuration)
    {
        var explicitConnection = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrWhiteSpace(explicitConnection))
            return explicitConnection;

        var fromUrl = NpgsqlFromDatabaseUrl(configuration["DATABASE_URL"]);
        if (fromUrl is not null)
            return fromUrl;

        throw new InvalidOperationException(
            "No se encontró la cadena de conexión. Configurá 'ConnectionStrings:DefaultConnection' " +
            "(user-secrets en dev, variable de entorno en prod) o 'DATABASE_URL' si la plataforma la provee.");
    }

    /// <summary>
    /// Physical root for uploaded images. In production this must point at a
    /// mounted volume (<c>Storage:UploadsPath</c>), because a container filesystem
    /// is wiped on every deploy. Falls back to <c>wwwroot/uploads</c> for local work.
    /// </summary>
    public static string ResolveUploadsRoot(IConfiguration configuration, string webRootPath)
    {
        var configured = configuration["Storage:UploadsPath"];

        if (string.IsNullOrWhiteSpace(configured))
        {
            var fallback = Path.Combine(webRootPath, "uploads");
            Directory.CreateDirectory(fallback);
            return fallback;
        }

        // Un path relativo se resolvería contra el working directory y las imágenes
        // terminarían en el filesystem efímero del contenedor sin que nadie se entere.
        // Es exactamente el fallo que este ajuste viene a evitar, así que se corta acá.
        if (!Path.IsPathRooted(configured))
        {
            throw new InvalidOperationException(
                $"'Storage:UploadsPath' debe ser una ruta absoluta (recibido: '{configured}'). " +
                "En producción tiene que apuntar al volumen montado, por ejemplo /data/uploads.");
        }

        var root = Path.GetFullPath(configured);
        Directory.CreateDirectory(root);
        return root;
    }

    /// <summary>
    /// The URL to bind to when the platform dictates a port via <c>PORT</c>.
    /// Returns <c>null</c> when the host already knows what to listen on, so an
    /// explicit ASPNETCORE_URLS is never overridden.
    /// </summary>
    public static string? BindUrlFromPortVariable(IConfiguration configuration)
    {
        if (!string.IsNullOrWhiteSpace(configuration["ASPNETCORE_URLS"]))
            return null;

        var port = configuration["PORT"];
        if (string.IsNullOrWhiteSpace(port) || !int.TryParse(port, out var parsed))
            return null;

        return $"http://0.0.0.0:{parsed}";
    }
}
