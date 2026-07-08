using FluentValidation;
using FluentValidation.AspNetCore;
using Mical.Services.Implementations;
using Mical.Services.Interfaces;
using Resend;

namespace Mical.Extensions;

/// <summary>Registro centralizado de los servicios de la aplicación (DI).</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IPromotionService, PromotionService>();
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ISkuGenerator, SkuGenerator>();
        services.AddScoped<IFileStorageService, FileStorageService>();
        return services;
    }

    /// <summary>
    /// Registra el envío de emails: el cliente de Resend (API key desde configuración,
    /// nunca hardcodeada) detrás de la abstracción <see cref="IEmailService"/>, más el
    /// renderizador de plantillas HTML.
    /// </summary>
    public static IServiceCollection AddEmailServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddResend(options =>
        {
            // La API key se resuelve desde:
            //  - producción: variable de entorno RESEND_API_KEY
            //  - desarrollo: user-secrets Resend:ApiToken
            // Nunca se hardcodea.
            options.ApiToken = configuration["RESEND_API_KEY"]
                ?? configuration["Resend:ApiToken"]
                ?? string.Empty;
            // Con esto, un fallo del proveedor lanza excepción (lo capturamos y logueamos).
            options.ThrowExceptions = true;
        });

        services.AddScoped<IEmailTemplateRenderer, EmailTemplateRenderer>();
        services.AddScoped<IEmailService, ResendEmailService>();
        return services;
    }

    /// <summary>Registra FluentValidation (validación automática + adaptadores de cliente).</summary>
    public static IServiceCollection AddApplicationValidation(this IServiceCollection services)
    {
        services.AddFluentValidationAutoValidation();
        services.AddFluentValidationClientsideAdapters();
        services.AddValidatorsFromAssembly(typeof(ServiceCollectionExtensions).Assembly);
        return services;
    }
}
