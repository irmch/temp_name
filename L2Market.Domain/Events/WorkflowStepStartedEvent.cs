using System;

namespace L2Market.Domain.Events
{
    /// <summary>
    /// Событие начала шага workflow
    /// </summary>
    public class WorkflowStepStartedEvent
    {
        public string StepName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
