using System;
using System.Threading;
using System.Threading.Tasks;

namespace L2Market.Domain.Common
{
    public interface IEventBus
    {
        Task PublishAsync<T>(T @event, CancellationToken token = default);
        void Subscribe<T>(Func<T, Task> handler);
    }
}
