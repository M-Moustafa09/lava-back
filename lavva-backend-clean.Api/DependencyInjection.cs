using lavva_backend_clean.Api.Middleware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.DependencyInjection;
using System.Globalization;

namespace lavva_backend_clean.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddMemoryCache();
        services.AddDistributedMemoryCache();
        services.AddHttpContextAccessor();
        services.AddScoped<lavva_backend_clean.Application.Common.Interfaces.ICurrentUserService, lavva_backend_clean.Api.Services.CurrentUserService>();
        services.AddScoped<lavva_backend_clean.Application.Common.Interfaces.ISupportNotificationService, lavva_backend_clean.Api.Services.SupportNotificationService>();
        services.AddScoped<lavva_backend_clean.Application.Common.Interfaces.ICatalogChangeNotifier, lavva_backend_clean.Api.Services.CatalogChangeNotifier>();
        services.AddScoped<lavva_backend_clean.Application.Common.Interfaces.ICheckoutCityDirectory, lavva_backend_clean.Api.Services.CheckoutCityDirectory>();

        services.AddExceptionHandler<ValidationExceptionHandler>();
        services.AddProblemDetails();

        services.AddLocalization();
        services.AddSingleton<lavva_backend_clean.Shared.Resources.SharedResourceLocalizer>();
        services.AddSingleton<Microsoft.Extensions.Localization.IStringLocalizer<lavva_backend_clean.Shared.Resources.SharedResource>>(sp => sp.GetRequiredService<lavva_backend_clean.Shared.Resources.SharedResourceLocalizer>());
        services.AddSingleton<Microsoft.Extensions.Localization.IStringLocalizerFactory, lavva_backend_clean.Shared.Resources.SharedResourceLocalizerFactory>();

        services.Configure<RequestLocalizationOptions>(options =>
        {
            var supportedCultures = new[]
            {
                new CultureInfo("en"),
                new CultureInfo("ar")
            };

            options.DefaultRequestCulture = new RequestCulture("en");
            options.SupportedCultures = supportedCultures;
            options.SupportedUICultures = supportedCultures;
        });

        return services;
    }
}
