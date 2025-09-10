using System;

namespace L2Market.Domain.Events
{
    /// <summary>
    /// Событие успешного поиска процесса
    /// </summary>
    public class ProcessFoundEvent
    {
        public string ProcessName { get; set; } = string.Empty;
        public int ProcessId { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
