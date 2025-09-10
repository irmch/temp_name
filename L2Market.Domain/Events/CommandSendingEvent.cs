using System;

namespace L2Market.Domain.Events
{
    /// <summary>
    /// Событие отправки команды
    /// </summary>
    public class CommandSendingEvent
    {
        public string HexCommand { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
