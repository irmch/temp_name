using System;

namespace L2Market.Domain.Events
{
    /// <summary>
    /// Событие неудачного завершения workflow
    /// </summary>
    public class WorkflowFailedEvent
    {
        public string DllPath { get; set; } = string.Empty;
        public string ProcessName { get; set; } = string.Empty;
        public int ProcessId { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
