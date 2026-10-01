using lavva_backend_clean.Infrastructure.Persistence.Seeder.Categories;
using lavva_backend_clean.Infrastructure.Persistence.Seeder.Common;
using lavva_backend_clean.Infrastructure.Persistence.Seeder.Configs;
using lavva_backend_clean.Infrastructure.Persistence.Seeder.Countries;
using lavva_backend_clean.Infrastructure.Persistence.Seeder.Favorites;
using lavva_backend_clean.Infrastructure.Persistence.Seeder.Notifications;
using lavva_backend_clean.Infrastructure.Persistence.Seeder.Orders;
using lavva_backend_clean.Infrastructure.Persistence.Seeder.Products;
using lavva_backend_clean.Infrastructure.Persistence.Seeder.Reviews;
using lavva_backend_clean.Infrastructure.Persistence.Seeder.Roles;
using lavva_backend_clean.Infrastructure.Persistence.Seeder.SiteVisits;
using lavva_backend_clean.Infrastructure.Persistence.Seeder.Support;
using lavva_backend_clean.Infrastructure.Persistence.Seeder.Users;
using Microsoft.Extensions.DependencyInjection;

namespace lavva_backend_clean.Infrastructure.Persistence.Seeder;

public static class SeederExtensions
{
    public static IServiceCollection AddSeeders(this IServiceCollection services)
    {
        services.AddScoped<IDataSeeder, RoleSeeder>();
        services.AddScoped<IDataSeeder, CountrySeeder>();
        services.AddScoped<IDataSeeder, UserSeeder>();
        services.AddScoped<IDataSeeder, CategorySeeder>();
        services.AddScoped<IDataSeeder, ProductSeeder>();
        services.AddScoped<IDataSeeder, PageConfigSeeder>();
        services.AddScoped<IDataSeeder, OrderSeeder>();
        services.AddScoped<IDataSeeder, ReviewSeeder>();
        services.AddScoped<IDataSeeder, FavoriteSeeder>();
        services.AddScoped<IDataSeeder, NotificationSeeder>();
        services.AddScoped<IDataSeeder, SupportSeeder>();
        services.AddScoped<IDataSeeder, SiteVisitSeeder>();

        return services;
    }

    public static async Task SeedDatabaseAsync(
        this IServiceProvider serviceProvider, 
        CancellationToken cancellationToken = default)
    {
        await DatabaseSeeder.SeedAsync(serviceProvider, cancellationToken);
    }
}
