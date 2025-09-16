using L2Market.Core;
using L2Market.Core.Configuration;
using L2Market.Core.Services;
using L2Market.Domain;
using L2Market.Domain.Common;
using L2Market.Domain.Events;
using L2Market.Domain.Services;
using L2Market.Infrastructure;
using L2Market.UI.EventHandlers;
using L2Market.UI.ViewModels;
using L2Market.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Windows;

namespace L2Market.UI
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private ServiceProvider? _serviceProvider;
        
        public ServiceProvider? ServiceProvider => _serviceProvider;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Configure DI container
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            // Force creation of LogsViewModel first
            _ = _serviceProvider.GetRequiredService<LogsViewModel>();
            
            // Force creation of global services only
            _ = _serviceProvider.GetRequiredService<UiEventHandler>();
            
            // Force creation of tracking services (these are Singleton)
            _ = _serviceProvider.GetRequiredService<TrackingService>();
            _ = _serviceProvider.GetRequiredService<NotificationService>();
            
            // Force creation of ConnectionEventRouter to subscribe to global EventBus
            _ = _serviceProvider.GetRequiredService<ConnectionEventRouter>();
            
            // MarketWindowViewModel will be created when MarketWindow is opened

            // Create and show connections window
            var connectionsViewModel = new ConnectionsViewModel(
                _serviceProvider.GetRequiredService<IConnectionManager>(),
                _serviceProvider.GetRequiredService<IMultiProcessMonitor>(),
                _serviceProvider.GetRequiredService<IConfigurationService>(),
                _serviceProvider.GetRequiredService<ILogger<ConnectionsViewModel>>(),
                _serviceProvider.GetRequiredService<LogsViewModel>(),
                _serviceProvider);
                
            var connectionsWindow = new ConnectionsWindow
            {
                DataContext = connectionsViewModel
            };
            
            // Store service provider in application resources for access from ViewModels
            Current.Resources["ServiceProvider"] = _serviceProvider;
            
            connectionsWindow.Show();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Register logging
            services.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            // Register layers
            services.AddDomain();
            services.AddInfrastructure();
            services.AddCore();
            
            // Override ConnectionManager with UI Dispatcher
            services.AddSingleton<IConnectionManager>(provider =>
            {
                var logger = provider.GetRequiredService<ILogger<ConnectionManager>>();
                return new ConnectionManager(logger, action => Application.Current.Dispatcher.Invoke(action));
            });

                // Register UI
            services.AddTransient<SettingsWindow>();
            services.AddTransient<MainViewModel>();
            services.AddTransient<ConnectionsWindow>();
            services.AddSingleton<ConnectionsViewModel>();
            services.AddTransient<ConnectionMarketWindow>();
            services.AddTransient<MarketWindow>();
            services.AddScoped<MarketWindowViewModel>();
            services.AddTransient<LogsWindow>();
            services.AddSingleton<LogsViewModel>();

            // Register UI event handler (uses subscriptions) - Singleton to ensure single instance
            services.AddSingleton<UiEventHandler>();
            
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Stop all services before exit
            if (_serviceProvider != null)
            {
                try
                {
                    var namedPipeService = _serviceProvider.GetService<L2Market.Domain.Services.INamedPipeService>();
                    namedPipeService?.StopServer();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error stopping NamedPipe service: {ex.Message}");
                }
                
                _serviceProvider.Dispose();
                _serviceProvider = null;
            }
            
            base.OnExit(e);
        }
    }
}
