using System;

namespace L2Market.Domain.Events
{
    /// <summary>
    /// Событие начала выполнения workflow
    /// </summary>
    public class WorkflowStartedEvent
    {
        public string DllPath { get; set; } = string.Empty;
        public string ProcessName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
