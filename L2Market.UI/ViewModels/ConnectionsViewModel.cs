using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using L2Market.Core.Configuration;
using L2Market.Core.Services;
using L2Market.Domain.Models;
using L2Market.Domain.Services;
using L2Market.UI.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace L2Market.UI.ViewModels
{
    /// <summary>
    /// ViewModel for the connections management window
    /// </summary>
    public class ConnectionsViewModel : INotifyPropertyChanged
    {
        private readonly IConnectionManager _connectionManager;
        private readonly IMultiProcessMonitor _processMonitor;
        private readonly IConfigurationService _configurationService;
        private readonly ILogger<ConnectionsViewModel> _logger;
        private readonly LogsViewModel _logsViewModel;
        private readonly IServiceProvider _serviceProvider;
        
        private bool _isMonitoring;
        private string _statusText = "Ready";

        public ConnectionsViewModel(
            IConnectionManager connectionManager,
            IMultiProcessMonitor processMonitor,
            IConfigurationService configurationService,
            ILogger<ConnectionsViewModel> logger,
            LogsViewModel logsViewModel,
            IServiceProvider serviceProvider)
        {
            _connectionManager = connectionManager ?? throw new ArgumentNullException(nameof(connectionManager));
            _processMonitor = processMonitor ?? throw new ArgumentNullException(nameof(processMonitor));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _logsViewModel = logsViewModel ?? throw new ArgumentNullException(nameof(logsViewModel));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            // Initialize commands
            StartMonitoringCommand = new RelayCommand(StartMonitoring, () => !IsMonitoring);
            StopMonitoringCommand = new RelayCommand(StopMonitoring, () => IsMonitoring);
            OpenSettingsCommand = new RelayCommand(OpenSettings);
            RefreshCommand = new RelayCommand(Refresh);
            ReloadSettingsCommand = new RelayCommand(async () => await ReloadSettingsAsync());
            OpenLogsCommand = new RelayCommand(OpenLogs);
            OpenMarketCommand = new RelayCommand<ConnectionInfo>(OpenMarket);
            RemoveConnectionCommand = new RelayCommand<ConnectionInfo>(RemoveConnection);

            // Subscribe to events
            _connectionManager.ConnectionAdded += OnConnectionAdded;
            _connectionManager.ConnectionRemoved += OnConnectionRemoved;
            _connectionManager.ConnectionStatusChanged += OnConnectionStatusChanged;
            _processMonitor.ProcessFound += OnProcessFound;

            _logger.LogInformation("🚀 ConnectionsViewModel created as Singleton");
            
            // Load settings asynchronously
            _ = Task.Run(async () => 
            {
                await _configurationService.LoadSettingsAsync();
                // Update UI on main thread
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    LoadSettings();
                });
            });
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ICommand StartMonitoringCommand { get; }
        public ICommand StopMonitoringCommand { get; }
        public ICommand OpenSettingsCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ReloadSettingsCommand { get; }
        public ICommand OpenLogsCommand { get; }
        public ICommand OpenMarketCommand { get; }
        public ICommand RemoveConnectionCommand { get; }

        public bool IsMonitoring
        {
            get => _isMonitoring;
            set
            {
                _isMonitoring = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusText));
            }
        }

        public string StatusText
        {
            get => _statusText;
            set
            {
                _statusText = value;
                OnPropertyChanged();
            }
        }

        public int ConnectionCount => _connectionManager.ConnectionCount;
        public int ConnectedCount => _connectionManager.ConnectedCount;
        
        public ObservableCollection<ConnectionInfo> Connections => _connectionManager.Connections;

        private void LoadSettings()
        {
            var settings = _configurationService.Settings;
            _processMonitor.ProcessName = settings.Injection.DefaultProcessName;
            
            _logger.LogInformation("📋 Settings loaded - Process Name: {ProcessName}, DLL Path: {DllPath}", 
                settings.Injection.DefaultProcessName, settings.Injection.DefaultDllPath);
            
            Application.Current.Dispatcher.Invoke(() =>
            {
                _logsViewModel.AddLogEntry($"📋 Settings loaded - Process Name: {settings.Injection.DefaultProcessName}, DLL Path: {settings.Injection.DefaultDllPath}", "Information");
            });
        }

        public async Task ReloadSettingsAsync()
        {
            try
            {
                _logger.LogInformation("🔄 Reloading settings...");
                await _configurationService.LoadSettingsAsync();
                
                // Update UI on main thread
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    LoadSettings();
                    _logger.LogInformation("✅ Settings reloaded successfully");
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error reloading settings");
            }
        }

        private void StartMonitoring()
        {
            try
            {
                var settings = _configurationService.Settings;
                _logger.LogInformation("🚀 Starting monitoring - Process Name: {ProcessName}, DLL Path: {DllPath}", 
                    settings.Injection.DefaultProcessName, settings.Injection.DefaultDllPath);
                
                if (string.IsNullOrWhiteSpace(settings.Injection.DefaultDllPath))
                {
                    _logger.LogError("❌ Cannot start monitoring: DLL path not configured!");
                    StatusText = "Error: DLL path not configured. Please configure in Settings.";
                    return;
                }
                
                IsMonitoring = true;
                StatusText = "Starting monitoring...";
                
                _ = Task.Run(async () => await _processMonitor.StartMonitoringAsync());
                
                StatusText = $"Monitoring {_processMonitor.ProcessName}...";
                _logger.LogInformation("✅ Started monitoring for {ProcessName}", _processMonitor.ProcessName);
                
                // Add test message to logs
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _logsViewModel.AddLogEntry($"✅ Started monitoring for {_processMonitor.ProcessName}", "Information");
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error starting monitoring");
                StatusText = "Error starting monitoring";
                IsMonitoring = false;
            }
        }

        private void StopMonitoring()
        {
            try
            {
                StatusText = "Stopping monitoring...";
                
                _ = Task.Run(async () => await _processMonitor.StopMonitoringAsync());
                
                IsMonitoring = false;
                StatusText = "Monitoring stopped";
                _logger.LogInformation("Stopped monitoring");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping monitoring");
                StatusText = "Error stopping monitoring";
            }
        }

        private void OpenSettings()
        {
            try
            {
                var settingsWindow = new Views.SettingsWindow(_configurationService);
                settingsWindow.ShowDialog();
                
                // Reload settings after closing
                LoadSettings();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening settings");
            }
        }

        private void OpenLogs()
        {
            try
            {
                var app = Application.Current as App;
                var logsWindow = app?.ServiceProvider?.GetService(typeof(LogsWindow)) as LogsWindow;
                if (logsWindow != null)
                {
                    logsWindow.DataContext = _logsViewModel;
                    logsWindow.Show();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening logs window");
                MessageBox.Show($"Error opening logs: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Refresh()
        {
            try
            {
                OnPropertyChanged(nameof(ConnectionCount));
                OnPropertyChanged(nameof(ConnectedCount));
                StatusText = IsMonitoring ? $"Monitoring {_processMonitor.ProcessName}..." : "Ready";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing data");
            }
        }

        private void OpenMarket(ConnectionInfo? connection)
        {
            if (connection == null) return;

            try
            {
                if (!connection.IsConnected)
                {
                    MessageBox.Show($"Process {connection.DisplayName} is not connected. Please wait for connection to be established.", 
                        "Not Connected", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Create connection scope for this specific connection
                var connectionScope = new ConnectionScope(_serviceProvider, (uint)connection.ProcessId);
                
                // Open market window for this specific connection
                var marketWindow = new Views.ConnectionMarketWindow(connection, connectionScope);
                marketWindow.Show();
                
                _logger.LogInformation("Opened market window for connection {ConnectionId}", connection.ConnectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error opening market window for connection {ConnectionId}", connection.ConnectionId);
                MessageBox.Show($"Error opening market window: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void RemoveConnection(ConnectionInfo? connection)
        {
            if (connection == null) return;

            try
            {
                var result = MessageBox.Show($"Are you sure you want to remove connection for {connection.DisplayName}?", 
                    "Remove Connection", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    await _connectionManager.RemoveConnectionAsync(connection.ConnectionId);
                    _logger.LogInformation("Removed connection {ConnectionId}", connection.ConnectionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing connection {ConnectionId}", connection.ConnectionId);
                MessageBox.Show($"Error removing connection: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void OnProcessFound(object? sender, ProcessFoundEventArgs e)
        {
            try
            {
                _logger.LogInformation("🔍 Process found: {ProcessName} (PID: {ProcessId})", e.ProcessName, e.ProcessId);
                
                // Add test message to logs on UI thread
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _logsViewModel.AddLogEntry($"🔍 Process found: {e.ProcessName} (PID: {e.ProcessId})", "Information");
                });
                
                // Check if settings are loaded
                var settings = _configurationService.Settings;
                _logger.LogInformation("📋 Settings - DLL Path: {DllPath}, Process Name: {ProcessName}", 
                    settings.Injection.DefaultDllPath, settings.Injection.DefaultProcessName);
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _logsViewModel.AddLogEntry($"📋 Settings - DLL Path: {settings.Injection.DefaultDllPath}, Process Name: {settings.Injection.DefaultProcessName}", "Information");
                });
                
                if (string.IsNullOrWhiteSpace(settings.Injection.DefaultDllPath))
                {
                    _logger.LogError("❌ DLL path not configured! Please configure in Settings.");
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _logsViewModel.AddLogEntry("❌ DLL path not configured! Please configure in Settings.", "Error");
                    });
                    return;
                }

                // Check if DLL file exists
                if (!File.Exists(settings.Injection.DefaultDllPath))
                {
                    _logger.LogError("❌ DLL file not found: {DllPath}", settings.Injection.DefaultDllPath);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _logsViewModel.AddLogEntry($"❌ DLL file not found: {settings.Injection.DefaultDllPath}", "Error");
                    });
                    return;
                }

                _logger.LogInformation("✅ DLL file exists: {DllPath}", settings.Injection.DefaultDllPath);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _logsViewModel.AddLogEntry($"✅ DLL file exists: {settings.Injection.DefaultDllPath}", "Information");
                });
                
                // Add connection
                ConnectionInfo connection;
                try
                {
                    connection = await _connectionManager.AddConnectionAsync(e.ProcessId, e.ProcessName, e.WindowTitle);
                    _logger.LogInformation("📝 Connection added: {ConnectionId}", connection.ConnectionId);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _logsViewModel.AddLogEntry($"📝 Connection added: {connection.ConnectionId}", "Information");
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error adding connection for process {ProcessId}", e.ProcessId);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _logsViewModel.AddLogEntry($"❌ Error adding connection for process {e.ProcessId}: {ex.Message}", "Error");
                    });
                    return;
                }
                
                // Check if this is a new connection or existing one
                if (connection.ConnectionStatus == "Connecting...")
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _logsViewModel.AddLogEntry($"🆕 New connection created for process {e.ProcessId}", "Information");
                    });
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _logsViewModel.AddLogEntry($"⚠️ Process {e.ProcessId} already has a connection, skipping injection", "Warning");
                    });
                    return;
                }
                
                // Start injection for this connection
                _ = Task.Run(async () =>
                {
                    try
                    {
                        _logger.LogInformation("🚀 Starting injection for process {ProcessId}...", e.ProcessId);
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            _logsViewModel.AddLogEntry($"🚀 Starting injection for process {e.ProcessId}...", "Information");
                        });
                        
                        // Create a new scope for this connection
                        using var scope = _serviceProvider.CreateScope();
                        var applicationService = scope.ServiceProvider.GetRequiredService<IApplicationService>();
                        
                        var result = await applicationService.ExecuteInjectionForProcessAsync(e.ProcessId, e.ProcessName);
                        
                        if (result.Success)
                        {
                            // Update connection status
                            await _connectionManager.UpdateConnectionStatusAsync(connection.ConnectionId, true, "Connected");
                            _logger.LogInformation("✅ Successfully connected to process {ProcessId} in {Duration:F1}s", e.ProcessId, result.TotalDuration.TotalSeconds);
                            
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                _logsViewModel.AddLogEntry($"✅ Successfully connected to process {e.ProcessId} in {result.TotalDuration.TotalSeconds:F1}s", "Information");
                            });
                        }
                        else
                        {
                            await _connectionManager.UpdateConnectionStatusAsync(connection.ConnectionId, false, $"Failed: {result.ErrorMessage}");
                            _logger.LogError("❌ Failed to connect to process {ProcessId}: {Error}", e.ProcessId, result.ErrorMessage);
                            
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                _logsViewModel.AddLogEntry($"❌ Failed to connect to process {e.ProcessId}: {result.ErrorMessage}", "Error");
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "💥 Error connecting to process {ProcessId}", e.ProcessId);
                        await _connectionManager.UpdateConnectionStatusAsync(connection.ConnectionId, false, "Connection Failed");
                        
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            _logsViewModel.AddLogEntry($"💥 Error connecting to process {e.ProcessId}: {ex.Message}", "Error");
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "💥 Error handling process found event");
            }
        }

        private void OnConnectionAdded(object? sender, ConnectionInfo connection)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                OnPropertyChanged(nameof(ConnectionCount));
                OnPropertyChanged(nameof(ConnectedCount));
                OnPropertyChanged(nameof(Connections));
            });
        }

        private void OnConnectionRemoved(object? sender, ConnectionInfo connection)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                OnPropertyChanged(nameof(ConnectionCount));
                OnPropertyChanged(nameof(ConnectedCount));
                OnPropertyChanged(nameof(Connections));
            });
        }

        private void OnConnectionStatusChanged(object? sender, ConnectionInfo connection)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                OnPropertyChanged(nameof(ConnectedCount));
                OnPropertyChanged(nameof(Connections));
            });
        }

        public void Cleanup()
        {
            try
            {
                // Unsubscribe from events
                _connectionManager.ConnectionAdded -= OnConnectionAdded;
                _connectionManager.ConnectionRemoved -= OnConnectionRemoved;
                _connectionManager.ConnectionStatusChanged -= OnConnectionStatusChanged;
                _processMonitor.ProcessFound -= OnProcessFound;

                // Stop monitoring
                _ = Task.Run(async () => await _processMonitor.StopMonitoringAsync());
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
    }
}
