using L2Market.Core.Configuration;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace L2Market.UI.ViewModels
{
    /// <summary>
    /// ViewModel for settings window
    /// </summary>
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private readonly IConfigurationService _configurationService;
        private AppSettings _settings;
        private bool _hasChanges;

        public event PropertyChangedEventHandler? PropertyChanged;

        public AppSettings Settings
        {
            get => _settings;
            set
            {
                _settings = value;
                OnPropertyChanged();
            }
        }

        public bool HasChanges
        {
            get => _hasChanges;
            set
            {
                _hasChanges = value;
                OnPropertyChanged();
            }
        }

        // NamedPipe timeout properties (in seconds)
        public double ConnectionTimeoutSeconds
        {
            get => Settings.NamedPipe.ConnectionTimeout.TotalSeconds;
            set
            {
                Settings.NamedPipe.ConnectionTimeout = TimeSpan.FromSeconds(value);
                OnPropertyChanged();
            }
        }

        public double RetryDelaySeconds
        {
            get => Settings.NamedPipe.RetryDelay.TotalSeconds;
            set
            {
                Settings.NamedPipe.RetryDelay = TimeSpan.FromSeconds(value);
                OnPropertyChanged();
            }
        }

        public double ReadTimeoutSeconds
        {
            get => Settings.NamedPipe.ReadTimeout.TotalSeconds;
            set
            {
                Settings.NamedPipe.ReadTimeout = TimeSpan.FromSeconds(value);
                OnPropertyChanged();
            }
        }

        public double ServerShutdownTimeoutSeconds
        {
            get => Settings.NamedPipe.ServerShutdownTimeout.TotalSeconds;
            set
            {
                Settings.NamedPipe.ServerShutdownTimeout = TimeSpan.FromSeconds(value);
                OnPropertyChanged();
            }
        }

        // Injection timeout properties (in seconds)
        public double WorkflowTimeoutSeconds
        {
            get => Settings.Injection.WorkflowTimeout.TotalSeconds;
            set
            {
                Settings.Injection.WorkflowTimeout = TimeSpan.FromSeconds(value);
                OnPropertyChanged();
            }
        }

        public double ProcessSearchTimeoutSeconds
        {
            get => Settings.Injection.ProcessSearchTimeout.TotalSeconds;
            set
            {
                Settings.Injection.ProcessSearchTimeout = TimeSpan.FromSeconds(value);
                OnPropertyChanged();
            }
        }

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ResetCommand { get; }
        public ICommand BrowseDllCommand { get; }

        public SettingsViewModel(IConfigurationService configurationService)
        {
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            // Create a deep copy of settings to work with
            _settings = new AppSettings
            {
                NamedPipe = new NamedPipeSettings
                {
                    ConnectionTimeout = _configurationService.Settings.NamedPipe.ConnectionTimeout,
                    RetryDelay = _configurationService.Settings.NamedPipe.RetryDelay,
                    MaxRetries = _configurationService.Settings.NamedPipe.MaxRetries,
                    ReadTimeout = _configurationService.Settings.NamedPipe.ReadTimeout,
                    ServerShutdownTimeout = _configurationService.Settings.NamedPipe.ServerShutdownTimeout
                },
                Injection = new InjectionSettings
                {
                    WorkflowTimeout = _configurationService.Settings.Injection.WorkflowTimeout,
                    ProcessSearchTimeout = _configurationService.Settings.Injection.ProcessSearchTimeout,
                    DefaultProcessName = _configurationService.Settings.Injection.DefaultProcessName,
                    DefaultDllPath = _configurationService.Settings.Injection.DefaultDllPath
                },
                UI = new UISettings
                {
                    AutoScroll = _configurationService.Settings.UI.AutoScroll,
                    MaxLogLines = _configurationService.Settings.UI.MaxLogLines,
                    ShowTimestamps = _configurationService.Settings.UI.ShowTimestamps,
                    Theme = _configurationService.Settings.UI.Theme,
                    MinimizeToTray = _configurationService.Settings.UI.MinimizeToTray,
                    AutoStartEnabled = _configurationService.Settings.UI.AutoStartEnabled
                }
            };
            
            SaveCommand = new RelayCommand(async () => await SaveSettingsAsync(), () => HasChanges);
            CancelCommand = new RelayCommand(() => CancelChanges());
            ResetCommand = new RelayCommand(async () => await ResetToDefaultsAsync());
            BrowseDllCommand = new RelayCommand(BrowseDll);
        }

        private async Task SaveSettingsAsync()
        {
            try
            {
                // Update the original settings with our changes
                _configurationService.Settings.NamedPipe.ConnectionTimeout = Settings.NamedPipe.ConnectionTimeout;
                _configurationService.Settings.NamedPipe.RetryDelay = Settings.NamedPipe.RetryDelay;
                _configurationService.Settings.NamedPipe.MaxRetries = Settings.NamedPipe.MaxRetries;
                _configurationService.Settings.NamedPipe.ReadTimeout = Settings.NamedPipe.ReadTimeout;
                _configurationService.Settings.NamedPipe.ServerShutdownTimeout = Settings.NamedPipe.ServerShutdownTimeout;
                
                _configurationService.Settings.Injection.WorkflowTimeout = Settings.Injection.WorkflowTimeout;
                _configurationService.Settings.Injection.ProcessSearchTimeout = Settings.Injection.ProcessSearchTimeout;
                _configurationService.Settings.Injection.DefaultProcessName = Settings.Injection.DefaultProcessName;
                _configurationService.Settings.Injection.DefaultDllPath = Settings.Injection.DefaultDllPath;
                
                _configurationService.Settings.UI.AutoScroll = Settings.UI.AutoScroll;
                _configurationService.Settings.UI.MaxLogLines = Settings.UI.MaxLogLines;
                _configurationService.Settings.UI.ShowTimestamps = Settings.UI.ShowTimestamps;
                _configurationService.Settings.UI.Theme = Settings.UI.Theme;
                _configurationService.Settings.UI.MinimizeToTray = Settings.UI.MinimizeToTray;
                _configurationService.Settings.UI.AutoStartEnabled = Settings.UI.AutoStartEnabled;
                
                await _configurationService.SaveSettingsAsync();
                HasChanges = false;
                
                System.Diagnostics.Debug.WriteLine("Settings saved successfully to L2Market.ini");
            }
            catch (Exception ex)
            {
                // Handle error - in real app, show message box
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        private void CancelChanges()
        {
            // Reload settings from configuration service
            _settings = new AppSettings
            {
                NamedPipe = new NamedPipeSettings
                {
                    ConnectionTimeout = _configurationService.Settings.NamedPipe.ConnectionTimeout,
                    RetryDelay = _configurationService.Settings.NamedPipe.RetryDelay,
                    MaxRetries = _configurationService.Settings.NamedPipe.MaxRetries,
                    ReadTimeout = _configurationService.Settings.NamedPipe.ReadTimeout,
                    ServerShutdownTimeout = _configurationService.Settings.NamedPipe.ServerShutdownTimeout
                },
                Injection = new InjectionSettings
                {
                    WorkflowTimeout = _configurationService.Settings.Injection.WorkflowTimeout,
                    ProcessSearchTimeout = _configurationService.Settings.Injection.ProcessSearchTimeout,
                    DefaultProcessName = _configurationService.Settings.Injection.DefaultProcessName,
                    DefaultDllPath = _configurationService.Settings.Injection.DefaultDllPath
                },
                UI = new UISettings
                {
                    AutoScroll = _configurationService.Settings.UI.AutoScroll,
                    MaxLogLines = _configurationService.Settings.UI.MaxLogLines,
                    ShowTimestamps = _configurationService.Settings.UI.ShowTimestamps,
                    Theme = _configurationService.Settings.UI.Theme,
                    MinimizeToTray = _configurationService.Settings.UI.MinimizeToTray,
                    AutoStartEnabled = _configurationService.Settings.UI.AutoStartEnabled
                }
            };
            OnPropertyChanged(nameof(Settings));
            HasChanges = false;
        }

        private async Task ResetToDefaultsAsync()
        {
            try
            {
                await _configurationService.ResetToDefaultsAsync();
                _settings = new AppSettings
                {
                    NamedPipe = new NamedPipeSettings
                    {
                        ConnectionTimeout = _configurationService.Settings.NamedPipe.ConnectionTimeout,
                        RetryDelay = _configurationService.Settings.NamedPipe.RetryDelay,
                        MaxRetries = _configurationService.Settings.NamedPipe.MaxRetries,
                        ReadTimeout = _configurationService.Settings.NamedPipe.ReadTimeout,
                        ServerShutdownTimeout = _configurationService.Settings.NamedPipe.ServerShutdownTimeout
                    },
                    Injection = new InjectionSettings
                    {
                        WorkflowTimeout = _configurationService.Settings.Injection.WorkflowTimeout,
                        ProcessSearchTimeout = _configurationService.Settings.Injection.ProcessSearchTimeout,
                        DefaultProcessName = _configurationService.Settings.Injection.DefaultProcessName,
                        DefaultDllPath = _configurationService.Settings.Injection.DefaultDllPath
                    },
                    UI = new UISettings
                    {
                        AutoScroll = _configurationService.Settings.UI.AutoScroll,
                        MaxLogLines = _configurationService.Settings.UI.MaxLogLines,
                        ShowTimestamps = _configurationService.Settings.UI.ShowTimestamps,
                        Theme = _configurationService.Settings.UI.Theme,
                        MinimizeToTray = _configurationService.Settings.UI.MinimizeToTray
                    }
                };
                OnPropertyChanged(nameof(Settings));
                HasChanges = false;
            }
            catch (Exception ex)
            {
                // Handle error
                System.Diagnostics.Debug.WriteLine($"Error resetting settings: {ex.Message}");
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
                Settings.Injection.DefaultDllPath = openFileDialog.FileName;
                OnPropertyChanged(nameof(Settings));
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            
            if (propertyName != nameof(HasChanges))
            {
                HasChanges = true;
            }
        }
    }

    /// <summary>
    /// Simple relay command implementation
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object? parameter) => _execute();
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Func<T?, bool>? _canExecute;

        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;

        public void Execute(object? parameter) => _execute((T?)parameter);
    }
}
