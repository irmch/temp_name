using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using L2Market.Core.Services;
using L2Market.Domain.Models;
using L2Market.Domain.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace L2Market.UI.ViewModels
{
    /// <summary>
    /// ViewModel for a market window connected to a specific process
    /// </summary>
    public class ConnectionMarketViewModel : INotifyPropertyChanged
    {
        private readonly ConnectionInfo _connectionInfo;
        private readonly ILogger<ConnectionMarketViewModel> _logger;
        private readonly ConnectionScope _connectionScope;
        private MarketWindowViewModel? _marketViewModel;
        private string _marketStatus = "Initializing...";
        private int _itemCount = 0;
        private string _lastUpdateTime = "Never";

        public ConnectionMarketViewModel(ConnectionInfo connectionInfo, ConnectionScope connectionScope)
        {
            _connectionInfo = connectionInfo ?? throw new ArgumentNullException(nameof(connectionInfo));
            _connectionScope = connectionScope ?? throw new ArgumentNullException(nameof(connectionScope));
            
            // Get logger from connection scope
            _logger = _connectionScope.GetService<ILogger<ConnectionMarketViewModel>>();

            // Initialize market view model for this specific connection
            InitializeMarketViewModel();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ConnectionInfo ConnectionInfo => _connectionInfo;

        public MarketWindowViewModel? MarketWindow
        {
            get => _marketViewModel;
            set
            {
                _marketViewModel = value;
                OnPropertyChanged();
            }
        }

        public string MarketStatus
        {
            get => _marketStatus;
            set
            {
                _marketStatus = value;
                OnPropertyChanged();
            }
        }

        public int ItemCount
        {
            get => _itemCount;
            set
            {
                _itemCount = value;
                OnPropertyChanged();
            }
        }

        public string LastUpdateTime
        {
            get => _lastUpdateTime;
            set
            {
                _lastUpdateTime = value;
                OnPropertyChanged();
            }
        }

        private void InitializeMarketViewModel()
        {
            try
            {
                // Create MarketWindowViewModel manually with the same ILocalEventBus as other services
                var serviceProvider = Application.Current.Resources["ServiceProvider"] as IServiceProvider;
                if (serviceProvider == null)
                {
                    throw new InvalidOperationException("ServiceProvider not found in application resources");
                }
                
                // Get services from the connection scope
                var localEventBus = _connectionScope.GetService<ILocalEventBus>();
                var trackingService = serviceProvider.GetRequiredService<TrackingService>();
                var notificationService = serviceProvider.GetRequiredService<NotificationService>();
                var logger = _connectionScope.GetService<ILogger<MarketWindowViewModel>>();
                
                // Create CommandService and MarketQueryService for this specific market window
                var pipeService = serviceProvider.GetRequiredService<INamedPipeService>();
                var commandLogger = _connectionScope.GetService<ILogger<CommandService>>();
                var commandService = new CommandService(pipeService, localEventBus, commandLogger, (uint)_connectionInfo.ProcessId);
                
                var marketQueryLogger = _connectionScope.GetService<ILogger<MarketQueryService>>();
                var marketQueryService = new MarketQueryService(localEventBus, commandService, marketQueryLogger);
                
                // Get MarketManagerService from ConnectionScope (it's already created there)
                var marketManager = _connectionScope.GetService<MarketManagerService>();
                
                // Create MarketWindowViewModel with the same ILocalEventBus
                _marketViewModel = new MarketWindowViewModel(
                    trackingService,
                    marketManager,
                    notificationService,
                    marketQueryService,
                    localEventBus,
                    _connectionInfo.ConnectionId);
                
                // Subscribe to property changes
                _marketViewModel.PropertyChanged += OnMarketViewModelPropertyChanged;
                
                MarketStatus = "Market view initialized successfully";
                _logger.LogInformation("Market view model created for connection {ConnectionId}", _connectionInfo.ConnectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing market view model for connection {ConnectionId}", _connectionInfo.ConnectionId);
                MarketStatus = $"Error: {ex.Message}";
            }
        }

        private void OnMarketViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            try
            {
                if (e.PropertyName == nameof(MarketWindowViewModel.MarketItems))
                {
                    ItemCount = _marketViewModel?.MarketItems?.Count ?? 0;
                    LastUpdateTime = DateTime.Now.ToString("HH:mm:ss");
                }
                else if (e.PropertyName == nameof(MarketWindowViewModel.TotalItems))
                {
                    ItemCount = _marketViewModel?.TotalItems ?? 0;
                    LastUpdateTime = DateTime.Now.ToString("HH:mm:ss");
                }
                else if (e.PropertyName == nameof(MarketWindowViewModel.LastUpdateTime))
                {
                    LastUpdateTime = _marketViewModel?.LastUpdateTime ?? "Never";
                }
                
                // Update market status
                if (_marketViewModel != null)
                {
                    MarketStatus = $"Connected - {ItemCount} items";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling market view model property change");
            }
        }

        public void Cleanup()
        {
            try
            {
                if (_marketViewModel != null)
                {
                    _marketViewModel.PropertyChanged -= OnMarketViewModelPropertyChanged;
                    // TODO: Add Cleanup method to MarketWindowViewModel when implemented
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup");
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            _connectionScope?.Dispose();
        }
    }
}
