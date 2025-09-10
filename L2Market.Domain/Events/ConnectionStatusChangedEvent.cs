using System;

namespace L2Market.Domain.Events
{
    /// <summary>
    /// Событие изменения статуса подключения
    /// </summary>
    public class ConnectionStatusChangedEvent
    {
        public bool IsConnected { get; set; }
        public string ConnectionName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public ConnectionStatusChangedEvent(bool isConnected, string connectionName)
        {
            IsConnected = isConnected;
            ConnectionName = connectionName;
            Timestamp = DateTime.UtcNow;
        }
    }
}
