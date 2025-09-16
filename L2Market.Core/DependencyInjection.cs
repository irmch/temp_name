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
            services.AddSingleton<IConfigurationService, IniConfigurationService>();
            
            // Register services
            services.AddScoped<IApplicationService, ApplicationService>();
            
            // Register market services - Scoped for each connection
            // These will be created with ProcessId in ConnectionScope
            services.AddScoped<PrivateStoreService>();
            services.AddScoped<CommissionService>();
            services.AddScoped<WorldExchangeService>();
            services.AddScoped<MarketManagerService>();
            
            // Register tracking services
            services.AddSingleton<TrackingService>();
            services.AddSingleton<NotificationService>();
            services.AddSingleton<ProfileService>();
            services.AddScoped<MarketQueryService>(); // Scoped for each connection
            
            // Register command service - will be created per connection
            services.AddScoped<ICommandService, CommandService>();
            
            // Multi-connection services
            services.AddSingleton<IConnectionManager, ConnectionManager>();
            services.AddSingleton<IMultiProcessMonitor, MultiProcessMonitor>();
            
            // Connection scope factory
            services.AddTransient<ConnectionScope>();
            
            // Local event bus - Scoped for each connection
            services.AddScoped<ILocalEventBus, LocalEventBus>();
            
            // Packet parser - Scoped for each connection
            services.AddScoped<IPacketParserService, PacketParserService>();
            
            // Connection event router - Singleton for routing global events
            services.AddSingleton<ConnectionEventRouter>();
            
            // All event handling is done via subscriptions, not DI handlers
            
            return services;
        }
    }
}