using System;

namespace L2Market.Domain.Events
{
    /// <summary>
    /// Событие успешной отправки команды
    /// </summary>
    public class CommandSentEvent
    {
        public string HexCommand { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
