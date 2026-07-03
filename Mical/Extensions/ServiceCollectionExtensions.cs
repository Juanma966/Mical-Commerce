using FluentValidation;
using FluentValidation.AspNetCore;
using Mical.Services.Implementations;
using Mical.Services.Interfaces;

namespace Mical.Extensions;

/// <summary>Registro centralizado de los servicios de la aplicación (DI).</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<ISkuGenerator, SkuGenerator>();
        services.AddScoped<IFileStorageService, FileStorageService>();
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
