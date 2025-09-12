using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using L2Market.Domain.Common;
using L2Market.Domain.Events;
using L2Market.Domain.Models;
using L2Market.Domain.Services;
using Microsoft.Extensions.Logging;

namespace L2Market.Core.Services
{
    /// <summary>
    /// Сервис автоматической отправки запросов на получение данных рынка
    /// </summary>
    public class MarketQueryService : IDisposable
    {
        private readonly IEventBus _eventBus;
        private readonly ICommandService _commandService;
        private readonly ILogger<MarketQueryService> _logger;
        private readonly Dictionary<MarketType, Timer> _trackingTimers;
        private readonly Dictionary<MarketType, List<TrackingRule>> _activeRules;
        private readonly object _lockObject = new object();

        public MarketQueryService(
            IEventBus eventBus,
            ICommandService commandService,
            ILogger<MarketQueryService> logger)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            _trackingTimers = new Dictionary<MarketType, Timer>();
            _activeRules = new Dictionary<MarketType, List<TrackingRule>>();
            
            // Инициализируем списки для каждого типа рынка
            foreach (MarketType marketType in Enum.GetValues<MarketType>())
            {
                if (marketType != MarketType.All)
                {
                    _activeRules[marketType] = new List<TrackingRule>();
                }
            }
        }

        /// <summary>
        /// Включает автоматическую отправку запросов для типа рынка
        /// </summary>
        public void StartQuerying(MarketType marketType, TimeSpan interval)
        {
            if (marketType == MarketType.All)
            {
                _logger.LogWarning("[MarketQueryService] Cannot start querying for 'All' market type");
                return;
            }

            lock (_lockObject)
            {
                if (_trackingTimers.ContainsKey(marketType))
                {
                    _logger.LogInformation("[MarketQueryService] Stopping existing timer for {MarketType}", marketType);
                    _trackingTimers[marketType]?.Dispose();
                }

                _logger.LogInformation("[MarketQueryService] Starting querying for {MarketType} with interval {Interval}", 
                    marketType, interval);

                _trackingTimers[marketType] = new Timer(
                    async _ => await ExecuteQueryAsync(marketType),
                    null,
                    TimeSpan.Zero, // Запускаем сразу
                    interval);

                _eventBus.PublishAsync(new LogMessageReceivedEvent(
                    $"[MarketQuery] Включена отправка запросов {GetMarketTypeName(marketType)} с интервалом {interval.TotalSeconds}с"));
            }
        }

        /// <summary>
        /// Выключает автоматическую отправку запросов для типа рынка
        /// </summary>
        public void StopQuerying(MarketType marketType)
        {
            if (marketType == MarketType.All)
            {
                _logger.LogWarning("[MarketQueryService] Cannot stop querying for 'All' market type");
                return;
            }

            lock (_lockObject)
            {
                if (_trackingTimers.ContainsKey(marketType))
                {
                    _logger.LogInformation("[MarketQueryService] Stopping querying for {MarketType}", marketType);
                    _trackingTimers[marketType]?.Dispose();
                    _trackingTimers.Remove(marketType);

                    _eventBus.PublishAsync(new LogMessageReceivedEvent(
                        $"[MarketQuery] Отключена отправка запросов {GetMarketTypeName(marketType)}"));
                }
            }
        }

        /// <summary>
        /// Добавляет правило отслеживания
        /// </summary>
        public void AddRule(TrackingRule rule)
        {
            if (rule.MarketType == MarketType.All)
            {
                _logger.LogWarning("[AutoTrackingService] Cannot add rule for 'All' market type");
                return;
            }

            lock (_lockObject)
            {
                if (!_activeRules[rule.MarketType].Contains(rule))
                {
                    _activeRules[rule.MarketType].Add(rule);
                    _logger.LogInformation("[AutoTrackingService] Added rule for {MarketType}: {RuleName}", 
                        rule.MarketType, rule.Name);
                }
            }
        }

        /// <summary>
        /// Удаляет правило отслеживания
        /// </summary>
        public void RemoveRule(TrackingRule rule)
        {
            if (rule.MarketType == MarketType.All)
            {
                _logger.LogWarning("[AutoTrackingService] Cannot remove rule for 'All' market type");
                return;
            }

            lock (_lockObject)
            {
                _activeRules[rule.MarketType].Remove(rule);
                _logger.LogInformation("[AutoTrackingService] Removed rule for {MarketType}: {RuleName}", 
                    rule.MarketType, rule.Name);
            }
        }

        /// <summary>
        /// Выполняет отправку запроса для указанного типа рынка
        /// </summary>
        private async Task ExecuteQueryAsync(MarketType marketType)
        {
            try
            {
                _logger.LogDebug("[MarketQueryService] Executing query for {MarketType}", marketType);

                // Отправляем простой запрос для получения данных
                await SendMarketQueryAsync(marketType);

                await _eventBus.PublishAsync(new LogMessageReceivedEvent(
                    $"[MarketQuery] Отправлен запрос данных {GetMarketTypeName(marketType)}"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MarketQueryService] Error in ExecuteQueryAsync for {MarketType}", marketType);
            }
        }

        /// <summary>
        /// Отправляет запрос данных для типа рынка
        /// </summary>
        private async Task SendMarketQueryAsync(MarketType marketType)
        {
            // Сначала открываем соответствующий магазин
            try
            {
                switch (marketType)
                {
                    case MarketType.Commission:
                        _logger.LogDebug("[MarketQueryService] Opening commission...");
                        
                        // Сначала дважды кликаем по NPC для открытия комиссии
                        _logger.LogDebug("[MarketQueryService] Sending first action command to NPC...");
                        await _commandService.SendActionCommandAsync(1210072924, 18805, 145867, -3077, 0);
                        await Task.Delay(500); // Небольшая задержка между кликами
                        
                        _logger.LogDebug("[MarketQueryService] Sending second action command to NPC...");
                        await _commandService.SendActionCommandAsync(1210072924, 18805, 145867, -3077, 0);
                        await Task.Delay(500); // Задержка перед bypass командой
                        
                        // Затем отправляем bypass команду
                        await _commandService.SendBypassCommandAsync("268458944", "show_commission");
                        
                        // Ждем, чтобы магазин успел открыться
                        await Task.Delay(2000);
                        
                        // Отправляем запросы для всех типов предметов (0-5)
                        for (int itemType = 0; itemType <= 5; itemType++)
                        {
                            try
                            {
                                await _commandService.SendCommissionBuyCommandAsync(itemType);
                                _logger.LogDebug("[MarketQueryService] Sent commission buy info request for ItemType: {ItemType}", itemType);
                                
                                // Задержка между запросами
                                await Task.Delay(5000);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "[MarketQueryService] Error sending request for ItemType: {ItemType}", itemType);
                            }
                        }
                        break;
                    case MarketType.PrivateStore:
                        _logger.LogDebug("[MarketQueryService] Opening private store window...");
                        
                        // Сначала открываем окно приватных магазинов
                        await _commandService.SendPrivateStoreWindowCommandAsync();
                        await Task.Delay(1000); // Задержка для открытия окна
                        
                        // Затем отправляем запросы для всех типов предметов (0-4)
                        for (int itemType = 0; itemType <= 4; itemType++)
                        {
                            try
                            {
                                await _commandService.SendItemListCommandAsync((byte)itemType);
                                _logger.LogDebug("[MarketQueryService] Sent private store item list request for ItemType: {ItemType}", itemType);
                                
                                // Задержка между запросами
                                await Task.Delay(5000);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "[MarketQueryService] Error sending private store request for ItemType: {ItemType}", itemType);
                            }
                        }
                        break;
                    case MarketType.WorldExchange:
                        _logger.LogDebug("[MarketQueryService] Opening world exchange...");
                        
                        // Сначала отправляем специальный первый пакет
                        await _commandService.SendWorldExchangeSearchCommandAsync(0x18, 0x0800);
                        await Task.Delay(1000); // Задержка после первого пакета
                        
                        // Затем отправляем запросы для всех типов предметов (0x00-0x19)
                        byte[] worldExchangeItemTypes = { 0x02, 0x01, 0x00, 0x07, 0x06, 0x05, 0x04, 0x08, 0x0B, 0x0A, 0x09, 0x0C, 0x0E, 0x0D, 0x19, 0x17, 0x10, 0x0F };
                        
                        for (int i = 0; i < worldExchangeItemTypes.Length; i++)
                        {
                            try
                            {
                                await _commandService.SendWorldExchangeSearchCommandAsync(worldExchangeItemTypes[i]);
                                _logger.LogDebug("[MarketQueryService] Sent world exchange search request for ItemType: 0x{ItemType:X2}", worldExchangeItemTypes[i]);
                                
                                // Задержка между запросами
                                await Task.Delay(5000);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "[MarketQueryService] Error sending world exchange request for ItemType: 0x{ItemType:X2}", worldExchangeItemTypes[i]);
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MarketQueryService] Error opening {MarketType}", marketType);
                return; // Не продолжаем, если не удалось открыть магазин
            }

            _logger.LogDebug("[MarketQueryService] Sent all market queries for {MarketType}", marketType);
        }

        /// <summary>
        /// Получает название типа рынка
        /// </summary>
        private string GetMarketTypeName(MarketType marketType)
        {
            return marketType switch
            {
                MarketType.PrivateStore => "Частные магазины",
                MarketType.Commission => "Комиссии",
                MarketType.WorldExchange => "Мировой обмен",
                _ => marketType.ToString()
            };
        }


        /// <summary>
        /// Отправляет запрос для всех типов предметов (0-5)
        /// </summary>
        public async Task SendAllQueriesAsync()
        {
            await ExecuteQueryAsync(MarketType.Commission);
        }

        /// <summary>
        /// Получает статус отслеживания для типа рынка
        /// </summary>
        public bool IsTrackingActive(MarketType marketType)
        {
            if (marketType == MarketType.All) return false;
            
            lock (_lockObject)
            {
                return _trackingTimers.ContainsKey(marketType);
            }
        }

        /// <summary>
        /// Получает количество активных правил для типа рынка
        /// </summary>
        public int GetActiveRulesCount(MarketType marketType)
        {
            if (marketType == MarketType.All) return 0;
            
            lock (_lockObject)
            {
                return _activeRules[marketType].Count(r => r.IsEnabled);
            }
        }

        public void Dispose()
        {
            lock (_lockObject)
            {
                foreach (var timer in _trackingTimers.Values)
                {
                    timer?.Dispose();
                }
                _trackingTimers.Clear();
            }
        }
    }
}
