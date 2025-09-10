using System;

namespace L2Market.Domain.Events
{
    /// <summary>
    /// Событие неудачного поиска процесса
    /// </summary>
    public class ProcessNotFoundEvent
    {
        public string ProcessName { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
