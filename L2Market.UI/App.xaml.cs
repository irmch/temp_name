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

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Configure DI container
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            // Force creation of services to ensure subscriptions happen
            _ = _serviceProvider.GetRequiredService<UiEventHandler>();
            _ = _serviceProvider.GetRequiredService<IPacketParserService>();
            
            // Force creation of market services to ensure subscriptions happen
            _ = _serviceProvider.GetRequiredService<PrivateStoreService>();
            _ = _serviceProvider.GetRequiredService<CommissionService>();
            _ = _serviceProvider.GetRequiredService<WorldExchangeService>();
            _ = _serviceProvider.GetRequiredService<MarketManagerService>();
            
            // Force creation of tracking services
            _ = _serviceProvider.GetRequiredService<TrackingService>();
            _ = _serviceProvider.GetRequiredService<NotificationService>();
            
            // Force creation of MarketWindowViewModel to ensure subscriptions are active
            _ = _serviceProvider.GetRequiredService<MarketWindowViewModel>();

            // Create and show main window
            var mainWindow = new MainWindow
            {
                DataContext = _serviceProvider.GetRequiredService<MainViewModel>()
            };
            
            // Store service provider in application resources for access from ViewModels
            Current.Resources["ServiceProvider"] = _serviceProvider;
            
            mainWindow.Show();
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

                // Register UI
            services.AddTransient<SettingsWindow>();
            services.AddTransient<MainViewModel>();
            services.AddTransient<MarketWindow>();
            services.AddSingleton<MarketWindowViewModel>();

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
