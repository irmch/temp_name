using System;
using System.Threading;
using System.Threading.Tasks;

namespace L2Market.Core.Services
{
    /// <summary>
    /// Local event bus interface for connection-specific events
    /// </summary>
    public interface ILocalEventBus
    {
        Task PublishAsync<T>(T @event, CancellationToken token = default);
        void Subscribe<T>(Func<T, Task> handler);
        void Unsubscribe<T>(Func<T, Task> handler);
    }
}
