using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.FileProviders;
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

    // Plataformas tipo Railway asignan el puerto por la variable PORT y esperan
    // que la app escuche ahí; si no, el health check nunca pasa. Un ASPNETCORE_URLS
    // explícito tiene prioridad y no se pisa.
    var bindUrl = HostingConfig.BindUrlFromPortVariable(builder.Configuration);
    if (bindUrl is not null)
        builder.WebHost.UseUrls(bindUrl);

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
    builder.Services.AddEmailServices(builder.Configuration);

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
    // Acepta además el DATABASE_URL que publican las plataformas tipo Railway.
    var connectionString = HostingConfig.ResolveConnectionString(builder.Configuration);

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
        // Detrás de un proxy inverso: respeta X-Forwarded-For/Proto para que el
        // HTTPS redirect, la cookie Secure y la IP del rate limiter sean correctas.
        //
        // En un PaaS (Railway, Render, Fly) el edge tiene IPs dinámicas y no se
        // pueden enumerar, así que KnownProxies/KnownNetworks van vacíos. El límite
        // de un solo hop hace que solo se honre la entrada que agrega ese edge.
        // Si algún día se despliega sobre un proxy propio con IP fija, restringir acá.
        var fwd = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            ForwardLimit = 1
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

    // Las imágenes subidas se sirven desde su raíz física (un volumen montado en
    // producción), pero conservan la misma URL /uploads/... que ya está guardada
    // en la base. Va antes del UseStaticFiles general para que gane esta ruta.
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new PhysicalFileProvider(
            HostingConfig.ResolveUploadsRoot(app.Configuration, app.Environment.WebRootPath)),
        RequestPath = HostingConfig.UploadsRequestPath
    });

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

    // Arranque de la base: aplica las migraciones pendientes y luego siembra
    // roles + administrador. Ambas cosas son idempotentes. En un PaaS no hay una
    // shell donde correr `dotnet ef database update` a mano, así que migrar acá
    // es lo que hace que un deploy limpio levante contra una base vacía.
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();

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
