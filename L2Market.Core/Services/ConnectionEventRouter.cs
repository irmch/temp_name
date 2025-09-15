using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using L2Market.Domain.Common;
using L2Market.Domain.Events;
using Microsoft.Extensions.Logging;

namespace L2Market.Core.Services
{
    /// <summary>
    /// Routes global events to connection-specific local event buses
    /// </summary>
    public class ConnectionEventRouter
    {
        private readonly IEventBus _globalEventBus;
        private readonly ILogger<ConnectionEventRouter> _logger;
        private readonly ConcurrentDictionary<uint, ILocalEventBus> _localEventBuses = new();

        public ConnectionEventRouter(IEventBus globalEventBus, ILogger<ConnectionEventRouter> logger)
        {
            _globalEventBus = globalEventBus ?? throw new ArgumentNullException(nameof(globalEventBus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Subscribe to global events that need to be routed
            _globalEventBus.Subscribe<PipeDataReceivedEvent>(HandlePipeDataReceivedEvent);
            // Other events are now published directly to LocalEventBus, no routing needed

            _logger.LogInformation("ConnectionEventRouter initialized and subscribed to global events.");
        }

        public void RegisterConnection(uint processId, ILocalEventBus localEventBus)
        {
            _localEventBuses.AddOrUpdate(processId, localEventBus, (key, oldBus) => localEventBus);
            _logger.LogInformation("Registered LocalEventBus for ProcessId: {ProcessId}", processId);
        }

        public void UnregisterConnection(uint processId)
        {
            _localEventBuses.TryRemove(processId, out _);
            _logger.LogInformation("Unregistered LocalEventBus for ProcessId: {ProcessId}", processId);
        }

        private async Task HandlePipeDataReceivedEvent(PipeDataReceivedEvent globalEvent)
        {
            // Route to specific LocalEventBus based on ProcessId
            if (globalEvent.ProcessId.HasValue && _localEventBuses.TryGetValue(globalEvent.ProcessId.Value, out var localBus))
            {
                try
                {
                    await localBus.PublishAsync(globalEvent);
                    _logger.LogDebug("Routed PipeDataReceivedEvent to LocalEventBus for ProcessId {ProcessId}", globalEvent.ProcessId.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error routing PipeDataReceivedEvent to LocalEventBus for ProcessId {ProcessId}", globalEvent.ProcessId.Value);
                }
            }
            else
            {
                _logger.LogWarning("Could not route PipeDataReceivedEvent, ProcessId missing or LocalEventBus not found for ProcessId: {ProcessId}", globalEvent.ProcessId);
            }
        }

    }
}
