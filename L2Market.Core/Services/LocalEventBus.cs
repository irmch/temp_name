using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace L2Market.Core.Services
{
    /// <summary>
    /// Local event bus implementation for connection-specific events
    /// Isolated from global application events
    /// </summary>
    public class LocalEventBus : ILocalEventBus, IDisposable
    {
        private readonly ConcurrentDictionary<Type, List<Func<object, Task>>> _handlers = new();
        private readonly ConcurrentQueue<object> _eventQueue = new();
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private bool _disposed = false;

        public Task PublishAsync<T>(T @event, CancellationToken token = default)
        {
            if (_disposed)
                return Task.CompletedTask;

            if (@event != null)
            {
                _eventQueue.Enqueue(@event);
                
                // Debug logging
                System.Diagnostics.Debug.WriteLine($"[LocalEventBus] Published {typeof(T).Name}, Queue size: {_eventQueue.Count}");
                
                // Process events asynchronously
                _ = Task.Run(async () => await ProcessEventsAsync(token), token);
            }
            
            return Task.CompletedTask;
        }

        public void Subscribe<T>(Func<T, Task> handler)
        {
            if (_disposed)
                return;

            var handlers = _handlers.GetOrAdd(typeof(T), _ => new List<Func<object, Task>>());
            lock (handlers)
            {
                handlers.Add(obj => handler((T)obj));
            }
            
            // Debug logging
            System.Diagnostics.Debug.WriteLine($"[LocalEventBus] Subscribed to {typeof(T).Name}, Total handlers: {handlers.Count}");
        }

        public void Unsubscribe<T>(Func<T, Task> handler)
        {
            if (_disposed)
                return;

            if (_handlers.TryGetValue(typeof(T), out var handlers))
            {
                lock (handlers)
                {
                    handlers.RemoveAll(h => h.Target == handler.Target);
                }
            }
        }

        private async Task ProcessEventsAsync(CancellationToken cancellationToken)
        {
            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                while (_eventQueue.TryDequeue(out var @event))
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    try
                    {
                        if (_handlers.TryGetValue(@event.GetType(), out var handlers))
                        {
                            System.Diagnostics.Debug.WriteLine($"[LocalEventBus] Processing {@event.GetType().Name} with {handlers.Count} handlers");
                            var tasks = new List<Task>();
                            lock (handlers)
                            {
                                foreach (var handler in handlers)
                                {
                                    tasks.Add(handler(@event));
                                }
                            }
                            await Task.WhenAll(tasks);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log error but continue processing
                        Console.WriteLine($"Error processing event in LocalEventBus: {ex.Message}");
                    }
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _semaphore.Dispose();
            }
        }
    }
}
