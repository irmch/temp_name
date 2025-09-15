using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace L2Market.UI.ViewModels
{
    /// <summary>
    /// ViewModel for individual log entry
    /// </summary>
    public class LogEntryViewModel : INotifyPropertyChanged
    {
        private DateTime _timestamp;
        private string _message = string.Empty;
        private string _level = string.Empty;

        public DateTime Timestamp
        {
            get => _timestamp;
            set
            {
                _timestamp = value;
                OnPropertyChanged();
            }
        }

        public string Message
        {
            get => _message;
            set
            {
                _message = value;
                OnPropertyChanged();
            }
        }

        public string Level
        {
            get => _level;
            set
            {
                _level = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TextColor));
            }
        }

        public string TextColor
        {
            get
            {
                return Level switch
                {
                    "Error" => "#dc3545",
                    "Warning" => "#ffc107",
                    "Information" => "#17a2b8",
                    "Debug" => "#6c757d",
                    _ => "#212529"
                };
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
