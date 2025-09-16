using System.ComponentModel;

namespace L2Market.Domain.Models
{
    /// <summary>
    /// Профиль игрока с настройками маркета
    /// </summary>
    public class PlayerProfile : INotifyPropertyChanged
    {
        private string _playerName = string.Empty;
        private string _server = "Cadmus";
        private bool _isPrivateStoreTrackingEnabled = false;
        private bool _isCommissionTrackingEnabled = false;
        private bool _isWorldExchangeTrackingEnabled = false;
        private bool _autoStartTracking = false;

        public string PlayerName
        {
            get => _playerName;
            set
            {
                _playerName = value;
                OnPropertyChanged();
            }
        }

        public string Server
        {
            get => _server;
            set
            {
                _server = value;
                OnPropertyChanged();
            }
        }

        public bool IsPrivateStoreTrackingEnabled
        {
            get => _isPrivateStoreTrackingEnabled;
            set
            {
                _isPrivateStoreTrackingEnabled = value;
                OnPropertyChanged();
            }
        }

        public bool IsCommissionTrackingEnabled
        {
            get => _isCommissionTrackingEnabled;
            set
            {
                _isCommissionTrackingEnabled = value;
                OnPropertyChanged();
            }
        }

        public bool IsWorldExchangeTrackingEnabled
        {
            get => _isWorldExchangeTrackingEnabled;
            set
            {
                _isWorldExchangeTrackingEnabled = value;
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

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
