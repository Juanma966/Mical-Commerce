namespace Mical.Extensions;

/// <summary>
/// Agrega cabeceras HTTP de seguridad a todas las respuestas.
/// Valores conservadores compatibles con una app MVC server-rendered
/// que carga Swiper y Google Fonts desde CDNs conocidos.
/// </summary>
public static class SecurityHeadersExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            var headers = context.Response.Headers;

            // Evita que el navegador "adivine" el tipo MIME (mitiga ataques de contenido).
            headers["X-Content-Type-Options"] = "nosniff";
            // Impide que el sitio se embeba en iframes de otros orígenes (anti clickjacking).
            headers["X-Frame-Options"] = "DENY";
            // Controla cuánta info de referer se envía a otros sitios.
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            // Desactiva APIs sensibles que la tienda no usa.
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

            await next();
        });
    }
}
