using System;

namespace L2Market.Domain.Events
{
    /// <summary>
    /// Событие начала инжекции DLL
    /// </summary>
    public class DllInjectionStartedEvent
    {
        public string DllPath { get; set; } = string.Empty;
        public int ProcessId { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
