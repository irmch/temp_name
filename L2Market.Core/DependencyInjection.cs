using L2Market.Core.Configuration;
using L2Market.Core.Services;
using L2Market.Domain;
using L2Market.Domain.Services;
using Microsoft.Extensions.DependencyInjection;

namespace L2Market.Core
{
    /// <summary>
    /// Extensions for Core layer service registration
    /// </summary>
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddCore(this IServiceCollection services)
        {
            // Register configuration
            services.AddSingleton<IConfigurationService, ConfigurationService>();
            
            // Register services
            services.AddScoped<IApplicationService, ApplicationService>();
            services.AddScoped<IPacketParserService, PacketParserService>();
            
            // Register market services
            services.AddScoped<PrivateStoreService>();
            services.AddScoped<CommissionService>();
            services.AddScoped<WorldExchangeService>();
            services.AddScoped<MarketManagerService>();
            
            // Register tracking services
            services.AddSingleton<TrackingService>();
            services.AddSingleton<NotificationService>();
            services.AddSingleton<AutoBuyService>();
            
            // All event handling is done via subscriptions, not DI handlers
            
            return services;
        }
    }
}