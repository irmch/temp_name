using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.Logging;

namespace L2Market.UI.ViewModels
{
    /// <summary>
    /// ViewModel for logs window
    /// </summary>
    public class LogsViewModel : INotifyPropertyChanged
    {
        private readonly ILogger<LogsViewModel> _logger;
        private bool _autoScroll = true;
        private int _maxLogEntries = 1000;

        public LogsViewModel(ILogger<LogsViewModel> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            LogEntries = new ObservableCollection<LogEntryViewModel>();
            
            // Initialize commands
            ClearLogsCommand = new RelayCommand(ClearLogs);
            SaveLogsCommand = new RelayCommand(SaveLogs);
            RefreshCommand = new RelayCommand(Refresh);
            ToggleAutoScrollCommand = new RelayCommand(ToggleAutoScroll);
            CloseCommand = new RelayCommand(Close);
            
            // Add initial test message
            AddLogEntry("📋 Logs window initialized", "Information");
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public ObservableCollection<LogEntryViewModel> LogEntries { get; }
        
        public int LogCount => LogEntries.Count;
        
        public bool AutoScroll
        {
            get => _autoScroll;
            set
            {
                _autoScroll = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(AutoScrollBackground));
            }
        }

        public string AutoScrollBackground => AutoScroll ? "#28a745" : "#6c757d";

        public ICommand ClearLogsCommand { get; }
        public ICommand SaveLogsCommand { get; }
        public ICommand RefreshCommand { get; }
        public ICommand ToggleAutoScrollCommand { get; }
        public ICommand CloseCommand { get; }

        public void AddLogEntry(string message, string level = "Information")
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[DEBUG] LogsViewModel: Adding log entry: {message}");
                
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var logEntry = new LogEntryViewModel
                    {
                        Timestamp = DateTime.Now,
                        Message = message,
                        Level = level
                    };

                    LogEntries.Add(logEntry);

                    // Limit log entries
                    while (LogEntries.Count > _maxLogEntries)
                    {
                        LogEntries.RemoveAt(0);
                    }

                    OnPropertyChanged(nameof(LogCount));
                    
                    System.Diagnostics.Debug.WriteLine($"[DEBUG] LogsViewModel: Log entry added successfully. Total count: {LogEntries.Count}");
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding log entry");
                System.Diagnostics.Debug.WriteLine($"[ERROR] LogsViewModel: Error adding log entry: {ex.Message}");
            }
        }

        private void ClearLogs()
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    LogEntries.Clear();
                    OnPropertyChanged(nameof(LogCount));
                    _logger.LogInformation("Logs cleared");
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing logs");
            }
        }

        private void SaveLogs()
        {
            try
            {
                var fileName = $"L2Market_Logs_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
                
                var logContent = string.Join(Environment.NewLine, 
                    LogEntries.Select(entry => $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{entry.Level}] {entry.Message}"));

                File.WriteAllText(filePath, logContent);
                
                MessageBox.Show($"Logs saved to: {filePath}", "Logs Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                _logger.LogInformation("Logs saved to {FilePath}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving logs");
                MessageBox.Show($"Error saving logs: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Refresh()
        {
            try
            {
                OnPropertyChanged(nameof(LogCount));
                _logger.LogInformation("Logs refreshed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing logs");
            }
        }

        private void ToggleAutoScroll()
        {
            AutoScroll = !AutoScroll;
            _logger.LogInformation("Auto scroll toggled: {AutoScroll}", AutoScroll);
        }

        private void Close()
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var window = Application.Current.Windows.OfType<Window>()
                        .FirstOrDefault(w => w.DataContext == this);
                    window?.Close();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing logs window");
            }
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
