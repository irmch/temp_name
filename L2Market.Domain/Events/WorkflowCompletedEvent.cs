using System;

namespace L2Market.Domain.Events
{
    /// <summary>
    /// Событие успешного завершения workflow
    /// </summary>
    public class WorkflowCompletedEvent
    {
        public string DllPath { get; set; } = string.Empty;
        public string ProcessName { get; set; } = string.Empty;
        public int ProcessId { get; set; }
        public TimeSpan Duration { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
