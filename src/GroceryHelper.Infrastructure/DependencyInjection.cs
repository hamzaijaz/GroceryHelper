using GroceryHelper.Application;
using GroceryHelper.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace GroceryHelper.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Singleton so the in-memory data lives for the lifetime of the app.
        services.AddSingleton<IGroceryRepository, InMemoryGroceryRepository>();
        return services;
    }
}
