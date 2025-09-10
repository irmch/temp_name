using System.Threading.Channels;
using L2Market.Domain.Common;

namespace L2Market.Infrastructure.EventBus;

public class InMemoryEventBus : IEventBus, IDisposable
{
    private readonly Channel<object> _channel = Channel.CreateUnbounded<object>();
    private readonly List<Func<object, Task>> _handlers = new();
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed;

    public InMemoryEventBus()
    {
        Task.Run(ProcessQueueAsync);
    }

    public async Task PublishAsync<T>(T @event, CancellationToken token = default)
    {
        if (@event != null)
        {
            await _channel.Writer.WriteAsync(@event, token);
        }
    }

    public void Subscribe<T>(Func<T, Task> handler)
    {
        SubscribeInternal(async (obj) =>
        {
            if (obj is T e) 
            {
                await handler(e);
            }
        });
    }

    private void SubscribeInternal(Func<object, Task> handler)
    {
        _handlers.Add(handler);
    }

    private async Task ProcessQueueAsync()
    {
        await foreach (var obj in _channel.Reader.ReadAllAsync(_cts.Token))
        {
            foreach (var handler in _handlers.ToArray())
            {
                _ = Task.Run(async () => 
                {
                    try
                    {
                        await handler(obj);
                    }
                    catch (Exception ex)
                    {
                        // Логирование ошибок
                        System.Diagnostics.Debug.WriteLine($"[EventBus] Error in handler: {ex.Message}");
                    }
                });
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _cts.Cancel();
        _channel.Writer.Complete();
        _disposed = true;
    }
}
