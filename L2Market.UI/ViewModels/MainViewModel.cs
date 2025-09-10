using L2Market.Core.Configuration;
using L2Market.Domain.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
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
        private string _dllPath;
        private string _processName;
        private bool _isLoading;
        private bool _isConnected;
        private System.Windows.Visibility _progressBarVisibility;
        private int _processId;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string DllPath
        {
            get => _dllPath;
            set
            {
                _dllPath = value;
                OnPropertyChanged();
            }
        }

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
                OnPropertyChanged(nameof(CanExecuteInjection));
            }
        }

        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                _isConnected = value;
                OnPropertyChanged();
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

        public bool CanExecuteInjection => !IsLoading && !string.IsNullOrWhiteSpace(DllPath);

        public ObservableCollection<string> LogMessages { get; } = new();

        public ICommand BrowseDllCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand ExecuteInjectionCommand { get; }
        public ICommand OpenSettingsCommand { get; }
        public ICommand OpenMarketCommand { get; }

        public MainViewModel(
            IApplicationService applicationService,
            INamedPipeService namedPipeService,
            IConfigurationService configurationService)
        {
            _applicationService = applicationService ?? throw new ArgumentNullException(nameof(applicationService));
            _namedPipeService = namedPipeService ?? throw new ArgumentNullException(nameof(namedPipeService));
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));

            // Initialize with default values from configuration
            var settings = _configurationService.Settings;
            _dllPath = settings.Injection.DefaultDllPath;
            _processName = settings.Injection.DefaultProcessName;

            // Initialize commands
            BrowseDllCommand = new RelayCommand(BrowseDll);
            ClearCommand = new RelayCommand(ClearFields);
            ExecuteInjectionCommand = new RelayCommand(async () => await ExecuteInjectionAsync(), () => CanExecuteInjection);
            OpenSettingsCommand = new RelayCommand(OpenSettings);
            OpenMarketCommand = new RelayCommand(OpenMarket);

            // Initialize connection status
            IsConnected = _namedPipeService.IsConnected;
        }

        // Add cleanup method
        public void Cleanup()
        {
            try
            {
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

        private void BrowseDll()
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Select DLL File",
                Filter = "DLL Files (*.dll)|*.dll|All Files (*.*)|*.*",
                DefaultExt = "dll"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                DllPath = openFileDialog.FileName;
                LogMessage($"DLL selected: {openFileDialog.FileName}");
            }
        }

        private void ClearFields()
        {
            DllPath = _configurationService.Settings.Injection.DefaultDllPath;
            ProcessName = _configurationService.Settings.Injection.DefaultProcessName;
            LogMessage("Fields cleared.");
        }

        private async Task ExecuteInjectionAsync()
        {
            if (string.IsNullOrWhiteSpace(DllPath) || DllPath == _configurationService.Settings.Injection.DefaultDllPath)
            {
                LogMessage("Please select a DLL file first.");
                return;
            }

            IsLoading = true;
            LogMessage("Starting injection workflow...");
            LogMessage("⚠️ Note: For DLL injection to work, this application must be run as Administrator");

            try
            {
                var result = await _applicationService.ExecuteInjectionWorkflowAsync(DllPath, ProcessName);
                
                if (result.Success)
                {
                    ProcessId = result.ProcessId;
                    LogMessage($"✓ Workflow completed successfully!");
                    LogMessage($"Process: {result.ProcessName} (PID: {result.ProcessId})");
                    LogMessage($"Duration: {result.TotalDuration.TotalSeconds:F1}s");
                    LogMessage("Ready to send commands via NamedPipe.");
                    IsConnected = result.NamedPipeConnected;
                }
                else
                {
                    LogMessage($"✗ Workflow failed: {result.ErrorMessage}");
                    IsConnected = false;
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error: {ex.Message}");
                IsConnected = false;
            }
            finally
            {
                IsLoading = false;
            }
        }


        private void OpenSettings()
        {
            var settingsWindow = new Views.SettingsWindow(_configurationService)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };
            settingsWindow.ShowDialog();
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

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
