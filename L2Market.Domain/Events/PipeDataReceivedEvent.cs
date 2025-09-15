using System;

namespace L2Market.Domain.Events
{
    /// <summary>
    /// Событие получения данных через Named Pipe
    /// </summary>
    public class PipeDataReceivedEvent
    {
        public string Data { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Source { get; set; } = string.Empty;
        public uint? ProcessId { get; set; }

        public PipeDataReceivedEvent(string data, string source = "", uint? processId = null)
        {
            Data = data;
            Source = source;
            ProcessId = processId;
            Timestamp = DateTime.UtcNow;
        }
    }
}
