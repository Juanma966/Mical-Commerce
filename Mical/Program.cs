using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Mical.Data;
using Mical.Data.Interceptors;
using Mical.Data.Seed;
using Mical.Entities;
using Mical.Extensions;
using Mical.Helpers;
using Serilog;

// Logger de arranque: captura errores incluso antes de construir el host.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Serilog como pipeline de logging, configurable desde appsettings.json.
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // Add services to the container.
    // Antiforgery global: valida el token en todo POST/PUT/DELETE (defensa CSRF).
    // Los endpoints de solo lectura que no lo necesitan usan [IgnoreAntiforgeryToken].
    builder.Services.AddControllersWithViews(options =>
        options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddApplicationServices();
    builder.Services.AddApplicationValidation();

    // Rate limiting: limita los intentos en los endpoints de autenticación
    // (login/registro) por IP, para frenar fuerza bruta y abuso automatizado.
    // Complementa el lockout de Identity (5 intentos / 15 min por cuenta).
    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy(RateLimitPolicies.Auth, httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1)
                }));
    });

    // Base de datos: PostgreSQL vía EF Core (Npgsql).
    // La cadena de conexión se resuelve desde configuración/entorno (nunca hardcodeada).
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "No se encontró la cadena de conexión 'DefaultConnection'. " +
            "Configurala con user-secrets (dev) o variables de entorno (prod).");

    // Interceptor de auditoría (registra acciones de admin en AuditLogs).
    builder.Services.AddScoped<AuditSaveChangesInterceptor>();

    builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
        options.UseNpgsql(connectionString)
               .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>())
               // OrderItem tiene navegación requerida a Product (soft-deletable, con
               // query filter). Usamos snapshots y nunca navegamos esa relación en
               // consultas filtradas, así que el warning no aplica.
               .ConfigureWarnings(w => w.Ignore(
                   CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning)));

    // ASP.NET Identity con autenticación por cookies (sin JWT).
    // Los flujos de registro/login se implementan en la Fase 1.2 y los roles en 1.3;
    // aquí solo se establece la infraestructura y las políticas base.
    builder.Services
        .AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            // Contraseñas: requisitos razonables sin ser hostiles.
            options.Password.RequiredLength = 8;
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = false;

            // Bloqueo por intentos fallidos (mitiga fuerza bruta).
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);

            // Cada email es único y el usuario debe tener email.
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

    // Google Login (esquema OAuth externo sobre la misma cookie de Identity).
    // Solo se activa si hay credenciales configuradas (por user-secrets/entorno),
    // así la app arranca igual sin ellas. Ver PRODUCTION.md.
    var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
    var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
    {
        builder.Services.AddAuthentication().AddGoogle(options =>
        {
            options.ClientId = googleClientId;
            options.ClientSecret = googleClientSecret;
        });
    }

    // Políticas de autorización.
    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy(Policies.AdminOnly, policy =>
            policy.RequireRole(Roles.Administrador));
    });

    // Configuración de la cookie de autenticación.
    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.Cookie.HttpOnly = true;                       // no accesible desde JS (mitiga XSS)
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromDays(14);
        options.SlidingExpiration = true;
        options.LoginPath = "/account/login";
        options.LogoutPath = "/account/logout";
        options.AccessDeniedPath = "/account/denied";
    });

    var app = builder.Build();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseDeveloperExceptionPage();
    }
    else
    {
        // Detrás de un proxy inverso (nginx, etc.): respeta X-Forwarded-For/Proto
        // para que HTTPS redirect, la cookie Secure y la IP del rate limiter sean correctas.
        // IMPORTANTE: en un despliegue real, restringir KnownProxies/KnownNetworks al proxy.
        var fwd = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        };
        fwd.KnownNetworks.Clear();
        fwd.KnownProxies.Clear();
        app.UseForwardedHeaders(fwd);

        app.UseExceptionHandler("/Home/Error");
        // HSTS: fuerza HTTPS en el navegador. 30 días por defecto.
        app.UseHsts();
    }

    // Errores por código de estado (404, 403, ...) → página amigable /Home/Error.
    app.UseStatusCodePagesWithReExecute("/Home/Error", "?statusCode={0}");

    // Log de cada request (método, ruta, status, tiempo).
    app.UseSerilogRequestLogging();

    app.UseHttpsRedirection();
    app.UseSecurityHeaders();
    app.UseStaticFiles();

    // Cultura invariante: los <input type="number"> envían decimales con punto
    // (formato invariante). Fijarla evita que el binding malinterprete "15000.50".
    var invariant = new[] { CultureInfo.InvariantCulture };
    app.UseRequestLocalization(new RequestLocalizationOptions
    {
        DefaultRequestCulture = new RequestCulture(CultureInfo.InvariantCulture),
        SupportedCultures = invariant,
        SupportedUICultures = invariant
    });

    app.UseRouting();

    app.UseRateLimiter();

    app.UseAuthentication();
    app.UseAuthorization();

    // Ruta de áreas (debe ir antes de la ruta por defecto). /Admin → Dashboard/Index.
    app.MapControllerRoute(
        name: "areas",
        pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

    // Seed inicial: roles de la aplicación + administrador. Idempotente.
    using (var scope = app.Services.CreateScope())
    {
        await DbInitializer.SeedAsync(scope.ServiceProvider);
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "La aplicación terminó inesperadamente durante el arranque.");
}
finally
{
    Log.CloseAndFlush();
}
