using System;

namespace L2Market.Domain.Events
{
    /// <summary>
    /// Событие успешного завершения инжекции DLL
    /// </summary>
    public class DllInjectionCompletedEvent
    {
        public string DllPath { get; set; } = string.Empty;
        public int ProcessId { get; set; }
        public string ProcessName { get; set; } = string.Empty;
        public TimeSpan Duration { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
