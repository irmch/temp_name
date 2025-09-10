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

        public SettingsViewModel(IConfigurationService configurationService)
        {
            _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
            _settings = _configurationService.Settings;
            
            SaveCommand = new RelayCommand(async () => await SaveSettingsAsync(), () => HasChanges);
            CancelCommand = new RelayCommand(() => CancelChanges());
            ResetCommand = new RelayCommand(async () => await ResetToDefaultsAsync());
        }

        private async Task SaveSettingsAsync()
        {
            try
            {
                _configurationService.Settings.NamedPipe = Settings.NamedPipe;
                _configurationService.Settings.Injection = Settings.Injection;
                _configurationService.Settings.UI = Settings.UI;
                
                await _configurationService.SaveSettingsAsync();
                HasChanges = false;
            }
            catch (Exception ex)
            {
                // Handle error - in real app, show message box
                System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        private void CancelChanges()
        {
            Settings = _configurationService.Settings;
            HasChanges = false;
        }

        private async Task ResetToDefaultsAsync()
        {
            try
            {
                await _configurationService.ResetToDefaultsAsync();
                Settings = _configurationService.Settings;
                HasChanges = false;
            }
            catch (Exception ex)
            {
                // Handle error
                System.Diagnostics.Debug.WriteLine($"Error resetting settings: {ex.Message}");
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
