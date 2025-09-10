using System;

namespace L2Market.Domain.Events
{
    /// <summary>
    /// Событие начала поиска процесса
    /// </summary>
    public class ProcessSearchStartedEvent
    {
        public string ProcessName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
