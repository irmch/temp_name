using System;

namespace L2Market.Domain.Events
{
    /// <summary>
    /// Событие ошибки в обработчике событий
    /// </summary>
    public class EventHandlerErrorEvent
    {
        public string EventType { get; set; } = string.Empty;
        public string HandlerType { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public Exception? Exception { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
