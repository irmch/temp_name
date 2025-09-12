using L2Market.Core.Configuration;
using L2Market.Core.Services;
using L2Market.Domain.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows.Input;

namespace L2Market.UI.ViewModels
{
    /// <summary>
    /// Main window ViewModel
    /// </summary>
    public class MainViewModel : INotifyPropertyChanged
    {
        private IApplicationService _applicationService;
        private INamedPipeService _namedPipeService;
        private IConfigurationService _configurationService;
        private ICommandService _commandService;
        private IDllInjectionService _dllInjectionService;
        private string _processName;
        private bool _isLoading;
        private bool _isConnected;
        private System.Windows.Visibility _progressBarVisibility;
        private int _processId;
        private string _chatMessage = string.Empty;
        private bool _autoStartEnabled = true;
        private CancellationTokenSource? _autoSearchCancellationTokenSource;
        private CancellationTokenSource? _connectionMonitorCancellationTokenSource;

        public event PropertyChangedEventHandler? PropertyChanged;


        public string ProcessName
        {
            get => _processName;
            set
            {
                _processName = value;
                OnPropertyChanged();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                _isLoading = value;
                ProgressBarVisibility = _isLoading ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
                OnPropertyChanged();
            }
        }

        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                var wasConnected = _isConnected;
                _isConnected = value;
                OnPropertyChanged();
                
                // If connection was lost, restart search automatically
                if (wasConnected && !value)
                {
                    LogMessage("🔌 Connection lost! Restarting automatic search...");
                    RestartSearch();
                }
            }
        }

        public System.Windows.Visibility ProgressBarVisibility
        {
            get => _progressBarVisibility;
            set
            {
                if (_progressBarVisibility != value)
                {
                    _progressBarVisibility = value;
                    OnPropertyChanged();
                }
            }
        }

        public int ProcessId
        {
            get => _processId;
            set
            {
                _processId = value;
                OnPropertyChanged();
            }
        }

        public bool AutoStartEnabled
        {
            get => _autoStartEnabled;
            set
            {
                _autoStartEnabled = value;
                OnPropertyChanged();
            }
        }


        public ObservableCollection<string> LogMessages { get; } = new();

        public ICommand ClearCommand { get; }
        public ICommand OpenSettingsCommand { get; }
        public ICommand OpenMarketCommand { get; }
        public ICommand SendChatMessageCommand { get; }
        public ICommand StopAutoSearchCommand { get; }
        public ICommand RestartSearchCommand { get; }

        public string ChatMessage
        {
            get => _chatMessage ?? string.Empty;
            set
            {
                _chatMessage = value;
                OnPropertyChanged();
            }
        }

        public MainViewModel(
            IApplicationService applicationService,
            INamedPipeService namedPipeService,
            IConfigurationService configurationService,
            ICommandService commandService,
            IDllInjectionService dllInjectionService)
        {
            _applicationService = applicationService ?? throw new ArgumentNullException(nameof(applicationService));
            _namedPipeService = namedPipeService ?? throw new ArgumentNullException(nameof(namedPipeService));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
            _dllInjectionService = dllInjectionService ?? throw new ArgumentNullException(nameof(dllInjectionService));

            // Initialize with default values from configuration
            var settings = _configurationService.Settings;
            _processName = settings.Injection.DefaultProcessName;
            
            // Load settings asynchronously and update UI
            _ = Task.Run(async () => 
            {
                await _configurationService.LoadSettingsAsync();
                // Update UI on main thread
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var updatedSettings = _configurationService.Settings;
                    ProcessName = updatedSettings.Injection.DefaultProcessName;
                    
                    // Update AutoStartEnabled from settings
                    AutoStartEnabled = updatedSettings.UI.AutoStartEnabled;
                    
                    // Start automatic search if enabled and settings are valid
                    if (AutoStartEnabled && !string.IsNullOrWhiteSpace(updatedSettings.Injection.DefaultDllPath))
                    {
                        LogMessage("🚀 Auto-start enabled! Starting automatic process search...");
                        StartAutomaticSearch();
                    }
                    else if (string.IsNullOrWhiteSpace(updatedSettings.Injection.DefaultDllPath))
                    {
                        LogMessage("⚠️ Auto-start disabled: DLL path not configured. Please configure in Settings.");
                    }
                    else if (!AutoStartEnabled)
                    {
                        LogMessage("⏸️ Auto-start disabled in settings. Click 'Restart Search' to start manually.");
                    }
                });
            });

            // Initialize commands
            ClearCommand = new RelayCommand(ClearFields);
            OpenSettingsCommand = new RelayCommand(OpenSettings);
            OpenMarketCommand = new RelayCommand(OpenMarket);
            SendChatMessageCommand = new RelayCommand(async () => await SendChatMessageAsync(), () => !string.IsNullOrWhiteSpace(ChatMessage));
            StopAutoSearchCommand = new RelayCommand(StopAutoSearch);
            RestartSearchCommand = new RelayCommand(RestartSearch);

            // Initialize connection status
            IsConnected = _namedPipeService.IsConnected;
            
            // Start connection monitoring
            StartConnectionMonitoring();
        }

        // Add cleanup method
        public void Cleanup()
        {
            try
            {
                // Stop auto-search if running
                if (_autoSearchCancellationTokenSource != null)
                {
                    _autoSearchCancellationTokenSource.Cancel();
                    _autoSearchCancellationTokenSource.Dispose();
                    _autoSearchCancellationTokenSource = null;
                }
                
                // Stop connection monitoring
                StopConnectionMonitoring();
                
                // Clear services first
                _applicationService = null!;
                _namedPipeService = null!;
                _configurationService = null!;
                
                // Clear log messages safely
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    LogMessages?.Clear();
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during ViewModel cleanup: {ex.Message}");
            }
        }


        private void ClearFields()
        {
            ProcessName = _configurationService.Settings.Injection.DefaultProcessName;
            LogMessage("Fields cleared.");
        }

        private void RestartSearch()
        {
            // Stop current search if running
            StopAutoSearch();
            
            // Wait a moment then start new search
            _ = Task.Run(async () =>
            {
                await Task.Delay(500); // Small delay to ensure cleanup
                
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    LogMessage("🔄 Restarting automatic search...");
                    StartAutomaticSearch();
                });
            });
        }


        private void OpenSettings()
        {
            var settingsWindow = new Views.SettingsWindow(_configurationService)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };
            settingsWindow.ShowDialog();
            
            // Refresh settings after closing settings window
            _ = Task.Run(async () => 
            {
                await _configurationService.LoadSettingsAsync();
                // Update UI on main thread
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    var settings = _configurationService.Settings;
                    ProcessName = settings.Injection.DefaultProcessName;
                });
            });
        }

        private void OpenMarket()
        {
            try
            {
                // Получаем MarketWindow из DI контейнера
                var serviceProvider = System.Windows.Application.Current.Resources["ServiceProvider"] as IServiceProvider;
                if (serviceProvider != null)
                {
                    var marketWindow = serviceProvider.GetService(typeof(Views.MarketWindow)) as Views.MarketWindow;
                    if (marketWindow != null)
                    {
                        // Проверяем, не открыто ли уже окно
                        if (marketWindow.Visibility == System.Windows.Visibility.Visible)
                        {
                            marketWindow.Activate();
                            return;
                        }
                        
                        marketWindow.Show();
                        marketWindow.WindowState = System.Windows.WindowState.Normal;
                        marketWindow.Activate();
                        LogMessage("✅ Окно рынка открыто");
                    }
                    else
                    {
                        LogMessage("❌ Не удалось создать окно рынка");
                    }
                }
                else
                {
                    LogMessage("❌ ServiceProvider не найден");
                }
            }
            catch (Exception ex)
            {
                LogMessage($"❌ Ошибка при открытии рынка: {ex.Message}");
                LogMessage($"❌ Детали ошибки: {ex.StackTrace}");
            }
        }

        public void LogMessage(string message)
        {
            try
            {
                string logEntry;
                
                if (_configurationService == null)
                {
                    // Fallback when configuration service is not available
                    logEntry = $"[{DateTime.Now:HH:mm:ss}] {message}";
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        LogMessages?.Add(logEntry);
                        
                        // Keep only the last 100 messages
                        if (LogMessages?.Count > 100)
                        {
                            LogMessages.RemoveAt(0);
                        }
                    });
                    return;
                }

                var settings = _configurationService.Settings.UI;
                var timestamp = settings.ShowTimestamps ? DateTime.Now.ToString("HH:mm:ss") : "";
                logEntry = string.IsNullOrEmpty(timestamp) ? message : $"[{timestamp}] {message}";
                
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    LogMessages?.Add(logEntry);
                    
                    // Auto-scroll if enabled
                    if (settings.AutoScroll)
                    {
                        // Scroll to last item
                    }
                    
                    // Limit log lines if configured
                    if (LogMessages?.Count > settings.MaxLogLines)
                    {
                        LogMessages.RemoveAt(0);
                    }
                });
            }
            catch (Exception ex)
            {
                // Fallback logging to debug output
                System.Diagnostics.Debug.WriteLine($"LogMessage error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Original message: {message}");
            }
        }

        private async Task SendChatMessageAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ChatMessage))
                    return;

                await _commandService.SendSay2CommandAsync(ChatMessage, 0);
                
                // Очищаем поле после отправки
                ChatMessage = string.Empty;
            }
            catch (Exception ex)
            {
                LogMessage($"Ошибка отправки сообщения: {ex.Message}");
            }
        }

        private void StartAutomaticSearch()
        {
            if (_autoSearchCancellationTokenSource != null)
            {
                LogMessage("⚠️ Auto-search already running!");
                return;
            }

            _autoSearchCancellationTokenSource = new CancellationTokenSource();
            _ = Task.Run(async () => await AutoSearchLoop(_autoSearchCancellationTokenSource.Token));
        }

        private void StopAutoSearch()
        {
            if (_autoSearchCancellationTokenSource != null)
            {
                _autoSearchCancellationTokenSource.Cancel();
                _autoSearchCancellationTokenSource.Dispose();
                _autoSearchCancellationTokenSource = null;
                LogMessage("🛑 Auto-search stopped by user");
            }
        }

        private async Task AutoSearchLoop(CancellationToken cancellationToken)
        {
            var settings = _configurationService.Settings;
            var processName = settings.Injection.DefaultProcessName;
            var searchInterval = TimeSpan.FromSeconds(5); // Check every 5 seconds
            var maxSearchTime = TimeSpan.FromMinutes(10); // Stop after 10 minutes
            var startTime = DateTime.UtcNow;

            LogMessage($"🔍 Auto-search started for process: {processName}");
            LogMessage("💡 Press 'Stop Auto Search' to cancel");

            while (!cancellationToken.IsCancellationRequested && DateTime.UtcNow - startTime < maxSearchTime)
            {
                try
                {
                    // Check if already connected
                    if (IsConnected)
                    {
                        LogMessage("✅ Already connected! Auto-search stopped.");
                        break;
                    }

                    // Check if process exists
                    var processResult = await _dllInjectionService.FindProcessAsync(processName);
                    if (processResult.Found)
                    {
                        LogMessage($"🎯 Process found! PID: {processResult.ProcessId}");
                        LogMessage("🚀 Starting automatic injection...");
                        
                        // Start injection
                        IsLoading = true;
                        try
                        {
                            var result = await _applicationService.ExecuteAutomaticInjectionAsync(cancellationToken);
                            
                            if (result.Success)
                            {
                                ProcessId = result.ProcessId;
                                LogMessage($"✅ Auto-injection completed successfully!");
                                LogMessage($"Process: {result.ProcessName} (PID: {result.ProcessId})");
                                LogMessage($"Duration: {result.TotalDuration.TotalSeconds:F1}s");
                                IsConnected = result.NamedPipeConnected;
                                break; // Success, stop searching
                            }
                            else
                            {
                                LogMessage($"❌ Auto-injection failed: {result.ErrorMessage}");
                                LogMessage("🔄 Continuing search...");
                            }
                        }
                        finally
                        {
                            IsLoading = false;
                        }
                    }
                    else
                    {
                        LogMessage($"⏳ Process '{processName}' not found, waiting...");
                    }

                    // Wait before next check
                    await Task.Delay(searchInterval, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    LogMessage("🛑 Auto-search cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    LogMessage($"❌ Auto-search error: {ex.Message}");
                    await Task.Delay(searchInterval, cancellationToken);
                }
            }

            if (DateTime.UtcNow - startTime >= maxSearchTime)
            {
                LogMessage("⏰ Auto-search timeout reached (10 minutes)");
            }

            // Clean up
            _autoSearchCancellationTokenSource?.Dispose();
            _autoSearchCancellationTokenSource = null;
        }

        private void StartConnectionMonitoring()
        {
            if (_connectionMonitorCancellationTokenSource != null)
            {
                return; // Already monitoring
            }

            _connectionMonitorCancellationTokenSource = new CancellationTokenSource();
            _ = Task.Run(async () => await ConnectionMonitorLoop(_connectionMonitorCancellationTokenSource.Token));
        }

        private void StopConnectionMonitoring()
        {
            if (_connectionMonitorCancellationTokenSource != null)
            {
                _connectionMonitorCancellationTokenSource.Cancel();
                _connectionMonitorCancellationTokenSource.Dispose();
                _connectionMonitorCancellationTokenSource = null;
            }
        }

        private async Task ConnectionMonitorLoop(CancellationToken cancellationToken)
        {
            var checkInterval = TimeSpan.FromSeconds(2); // Check every 2 seconds

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var isConnected = _namedPipeService.IsConnected;
                    
                    // Update UI on main thread
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        IsConnected = isConnected;
                    });

                    await Task.Delay(checkInterval, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    LogMessage($"❌ Connection monitor error: {ex.Message}");
                    await Task.Delay(checkInterval, cancellationToken);
                }
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
