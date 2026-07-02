using Microsoft.EntityFrameworkCore;
using Mical.Data;
using Mical.Extensions;
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

    // Base de datos: PostgreSQL vía EF Core (Npgsql).
    // La cadena de conexión se resuelve desde configuración/entorno (nunca hardcodeada).
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "No se encontró la cadena de conexión 'DefaultConnection'. " +
            "Configurala con user-secrets (dev) o variables de entorno (prod).");

    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));

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

    app.UseAuthorization();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}");

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
