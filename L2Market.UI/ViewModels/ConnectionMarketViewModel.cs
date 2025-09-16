using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using L2Market.Core.Services;
using L2Market.Domain.Models;
using L2Market.Domain.Services;
using L2Market.Domain.Events;
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
        private readonly ProfileService _profileService;
        private MarketWindowViewModel? _marketViewModel;
        private string _marketStatus = "Initializing...";
        private int _itemCount = 0;
        private string _lastUpdateTime = "Never";
        private string _playerName = "Unknown";
        private bool _autoStartTracking = false;
        private bool _profileLoaded = false;

        public ConnectionMarketViewModel(ConnectionInfo connectionInfo, ConnectionScope connectionScope)
        {
            _connectionInfo = connectionInfo ?? throw new ArgumentNullException(nameof(connectionInfo));
            _connectionScope = connectionScope ?? throw new ArgumentNullException(nameof(connectionScope));
            
            // Get logger and services from connection scope
            _logger = _connectionScope.GetService<ILogger<ConnectionMarketViewModel>>();
            _profileService = _connectionScope.GetService<ProfileService>();

            // Initialize commands
            OpenLogsCommand = new RelayCommand(OpenLogs);
            SaveProfileCommand = new RelayCommand(SaveProfile);

            // Subscribe to local event bus for player name updates
            var localEventBus = _connectionScope.GetService<ILocalEventBus>();
            localEventBus.Subscribe<LogMessageReceivedEvent>(OnLogMessageReceived);

            // Initialize market view model for this specific connection
            InitializeMarketViewModel();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ICommand OpenLogsCommand { get; }
        public ICommand SaveProfileCommand { get; }

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

        public string PlayerName
        {
            get => _playerName;
            set
            {
                _playerName = value;
                OnPropertyChanged();
            }
        }

        public bool AutoStartTracking
        {
            get => _autoStartTracking;
            set
            {
                _autoStartTracking = value;
                OnPropertyChanged();
            }
        }

        public bool ProfileLoaded
        {
            get => _profileLoaded;
            set
            {
                _profileLoaded = value;
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

        private async Task OnLogMessageReceived(LogMessageReceivedEvent logEvent)
        {
            try
            {
                // Проверяем, содержит ли сообщение информацию о нике игрока
                if (logEvent.Message.Contains("[PacketParserService] Player:"))
                {
                    // Извлекаем ник из сообщения
                    var parts = logEvent.Message.Split("Player:");
                    if (parts.Length > 1)
                    {
                        var playerName = parts[1].Trim();
                        if (!string.IsNullOrEmpty(playerName) && playerName != "Unknown")
                        {
                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                PlayerName = playerName;
                                // Загружаем профиль после получения ника
                                LoadProfile(playerName);
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing log message for player name");
            }
        }

        private void SaveProfile()
        {
            try
            {
                if (string.IsNullOrEmpty(PlayerName) || PlayerName == "Unknown")
                {
                    MessageBox.Show("Player name is not available. Please wait for the game to load.", 
                        "Cannot Save Profile", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (MarketWindow == null)
                {
                    MessageBox.Show("Market window is not initialized.", 
                        "Cannot Save Profile", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var profile = new PlayerProfile
                {
                    PlayerName = PlayerName,
                    Server = MarketWindow.SelectedServer,
                    IsPrivateStoreTrackingEnabled = MarketWindow.IsPrivateStoreTrackingEnabled,
                    IsCommissionTrackingEnabled = MarketWindow.IsCommissionTrackingEnabled,
                    IsWorldExchangeTrackingEnabled = MarketWindow.IsWorldExchangeTrackingEnabled,
                    AutoStartTracking = AutoStartTracking // Сохраняем текущее значение галочки
                };

                if (_profileService.SaveProfile(profile))
                {
                    MessageBox.Show($"Profile saved for {PlayerName}", 
                        "Profile Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show($"Failed to save profile for {PlayerName}", 
                        "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving profile");
                MessageBox.Show($"Error saving profile: {ex.Message}", 
                    "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadProfile(string playerName)
        {
            try
            {
                var profile = _profileService.LoadProfile(playerName);
                if (profile != null && MarketWindow != null)
                {
                    // Применяем настройки профиля
                    MarketWindow.SelectedServer = profile.Server;
                    MarketWindow.IsPrivateStoreTrackingEnabled = profile.IsPrivateStoreTrackingEnabled;
                    MarketWindow.IsCommissionTrackingEnabled = profile.IsCommissionTrackingEnabled;
                    MarketWindow.IsWorldExchangeTrackingEnabled = profile.IsWorldExchangeTrackingEnabled;
                    
                    // Устанавливаем автостарт из профиля
                    AutoStartTracking = profile.AutoStartTracking;
                    ProfileLoaded = true;

                    _logger.LogInformation("Profile loaded for {PlayerName}: Server={Server}, AutoStart={AutoStart}", 
                        profile.PlayerName, profile.Server, profile.AutoStartTracking);

                    // Автоматически запускаем отслеживание если включено в профиле
                    if (profile.AutoStartTracking)
                    {
                        _logger.LogInformation("Auto-starting tracking for {PlayerName}", playerName);
                        MarketWindow.StartTrackingCommand.Execute(null);
                    }
                }
                else
                {
                    _logger.LogDebug("No profile found for player: {PlayerName}", playerName);
                    ProfileLoaded = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading profile for player: {PlayerName}", playerName);
                ProfileLoaded = false;
            }
        }

        private void OpenLogs()
        {
            try
            {
                // Get the local event bus and logger from connection scope
                var localEventBus = _connectionScope.GetService<ILocalEventBus>();
                var logsLogger = _connectionScope.GetService<ILogger<LogsViewModel>>();
                
                // Create logs window for this specific connection
                var logsWindow = new Views.LogsWindow();
                var logsViewModel = new LogsViewModel(logsLogger, localEventBus, _connectionInfo.DisplayName);
                logsWindow.DataContext = logsViewModel;
                
                logsWindow.Show();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening logs window");
                MessageBox.Show($"Error opening logs: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
