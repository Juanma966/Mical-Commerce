using Mical.Services.Implementations;
using Mical.Services.Interfaces;

namespace Mical.Extensions;

/// <summary>Registro centralizado de los servicios de la aplicación (DI).</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ICategoryService, CategoryService>();
        return services;
    }
}
