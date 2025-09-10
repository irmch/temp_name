using System;

namespace L2Market.Domain.Events
{
    /// <summary>
    /// Событие неудачной отправки команды
    /// </summary>
    public class CommandFailedEvent
    {
        public string HexCommand { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
