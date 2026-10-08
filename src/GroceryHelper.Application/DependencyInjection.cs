using Microsoft.Extensions.DependencyInjection;

namespace GroceryHelper.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<GroceryService>();
        return services;
    }
}
