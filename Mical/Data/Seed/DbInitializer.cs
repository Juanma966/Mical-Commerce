using Mical.Entities;
using Mical.Helpers;
using Microsoft.AspNetCore.Identity;

namespace Mical.Data.Seed;

/// <summary>
/// Siembra los datos base imprescindibles: los roles de la aplicación y un
/// usuario administrador inicial. Es idempotente: se puede ejecutar en cada
/// arranque sin duplicar nada.
/// </summary>
public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var configuration = services.GetRequiredService<IConfiguration>();
        var logger = services.GetRequiredService<ILogger<Program>>();

        // 1) Roles.
        foreach (var role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
                logger.LogInformation("Rol creado: {Role}", role);
            }
        }

        // 2) Administrador inicial. Credenciales fuera del código:
        //    AdminSeed:Email / AdminSeed:FullName en appsettings, AdminSeed:Password en user-secrets/entorno.
        var email = configuration["AdminSeed:Email"];
        var password = configuration["AdminSeed:Password"];
        var fullName = configuration["AdminSeed:FullName"] ?? "Administrador";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            logger.LogWarning(
                "AdminSeed no está configurado (falta Email o Password). Se omite la creación del administrador inicial.");
            return;
        }

        var admin = await userManager.FindByEmailAsync(email);
        if (admin is null)
        {
            admin = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(admin, password);
            if (!result.Succeeded)
            {
                logger.LogError(
                    "No se pudo crear el administrador inicial: {Errors}",
                    string.Join("; ", result.Errors.Select(e => e.Description)));
                return;
            }

            logger.LogInformation("Administrador inicial creado: {Email}", email);
        }

        if (!await userManager.IsInRoleAsync(admin, Roles.Administrador))
        {
            await userManager.AddToRoleAsync(admin, Roles.Administrador);
            logger.LogInformation("Rol Administrador asignado a {Email}", email);
        }
    }
}
