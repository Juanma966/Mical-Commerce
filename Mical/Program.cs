using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Mical.Data;
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
    builder.Services.AddControllersWithViews();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddApplicationServices();

    // Base de datos: PostgreSQL vía EF Core (Npgsql).
    // La cadena de conexión se resuelve desde configuración/entorno (nunca hardcodeada).
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "No se encontró la cadena de conexión 'DefaultConnection'. " +
            "Configurala con user-secrets (dev) o variables de entorno (prod).");

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));

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

    app.UseRouting();

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
