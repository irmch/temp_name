using System;

namespace L2Market.Domain.Events
{
    /// <summary>
    /// Событие получения лог-сообщения
    /// </summary>
    public class LogMessageReceivedEvent
    {
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Source { get; set; } = string.Empty;

        public LogMessageReceivedEvent(string message, string source = "")
        {
            Message = message;
            Source = source;
            Timestamp = DateTime.UtcNow;
        }
    }
}
