using System;
using System.ComponentModel;

namespace L2Market.Domain.Models
{
    /// <summary>
    /// Information about a connection to a game process
    /// </summary>
    public class ConnectionInfo : INotifyPropertyChanged
    {
        private bool _isConnected;
        private string _connectionStatus = "Disconnected";
        private DateTime _connectedAt;
        private string _windowTitle = string.Empty;

        public Guid ConnectionId { get; set; } = Guid.NewGuid();
        
        public int ProcessId { get; set; }
        
        public string ProcessName { get; set; } = string.Empty;
        
        public string WindowTitle
        {
            get => _windowTitle;
            set
            {
                _windowTitle = value;
                OnPropertyChanged();
            }
        }
        
        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                _isConnected = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusIcon));
                OnPropertyChanged(nameof(StatusColor));
            }
        }
        
        public DateTime ConnectedAt
        {
            get => _connectedAt;
            set
            {
                _connectedAt = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FormattedConnectedAt));
            }
        }
        
        public string ConnectionStatus
        {
            get => _connectionStatus;
            set
            {
                _connectionStatus = value;
                OnPropertyChanged();
            }
        }
        
        public string FormattedConnectedAt => ConnectedAt.ToString("HH:mm:ss");
        
        public string StatusIcon => IsConnected ? "✅" : "❌";
        
        public string StatusColor => IsConnected ? "#28a745" : "#dc3545";
        
        public string DisplayName => $"{ProcessName} (PID: {ProcessId})";
        
        public string NamedPipeName => ProcessId.ToString();

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
