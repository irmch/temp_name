using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using L2Market.Domain.Common;
using System;
using L2Market.Domain.Services;

namespace L2Market.Core.Services
{
    /// <summary>
    /// Represents a scoped environment for a single connection
    /// </summary>
    public class ConnectionScope : IDisposable
    {
        private readonly IServiceScope _scope;
        private readonly ILocalEventBus _localEventBus;
        private readonly ConnectionEventRouter _eventRouter;
        private readonly uint _processId;
        private readonly PacketParserService _packetParser;
        private readonly PrivateStoreService _privateStore;
        private readonly CommissionService _commissionService;
        private readonly WorldExchangeService _worldExchangeService;
        private readonly MarketManagerService _marketManager;
        private bool _disposed = false;

        public uint ProcessId => _processId;

        public ConnectionScope(IServiceProvider serviceProvider, uint processId)
        {
            _processId = processId;
            _scope = serviceProvider.CreateScope();
            
            // Получаем LocalEventBus из Scope
            _localEventBus = _scope.ServiceProvider.GetRequiredService<ILocalEventBus>();
            
            // Получаем ConnectionEventRouter и регистрируем LocalEventBus
            _eventRouter = serviceProvider.GetRequiredService<ConnectionEventRouter>();
            _eventRouter.RegisterConnection(_processId, _localEventBus);
            
            // Создаем сервисы для этого подключения с ProcessId
            _packetParser = new PacketParserService(_localEventBus, _scope.ServiceProvider.GetRequiredService<ILogger<PacketParserService>>(), _processId);
            _privateStore = new PrivateStoreService(_localEventBus, _processId);
            _commissionService = new CommissionService(_localEventBus, _processId);
            _worldExchangeService = new WorldExchangeService(_localEventBus, _processId);
            _marketManager = new MarketManagerService(_privateStore, _commissionService, _worldExchangeService, _localEventBus);
        }

        public T GetService<T>() where T : class
        {
            // Special handling for services created in this scope
            if (typeof(T) == typeof(MarketManagerService))
                return (T)(object)_marketManager;
            if (typeof(T) == typeof(PrivateStoreService))
                return (T)(object)_privateStore;
            if (typeof(T) == typeof(CommissionService))
                return (T)(object)_commissionService;
            if (typeof(T) == typeof(WorldExchangeService))
                return (T)(object)_worldExchangeService;
            if (typeof(T) == typeof(PacketParserService))
                return (T)(object)_packetParser;
            
            return _scope.ServiceProvider.GetRequiredService<T>();
        }

        public T? GetOptionalService<T>() where T : class
        {
            return _scope.ServiceProvider.GetService<T>();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _eventRouter?.UnregisterConnection(_processId);
                // LocalEventBus не нужно Dispose, так как он управляется DI
                _scope?.Dispose();
                _disposed = true;
            }
        }
    }
}
