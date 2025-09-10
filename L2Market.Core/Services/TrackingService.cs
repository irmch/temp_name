using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using L2Market.Domain.Models;
using L2Market.Domain.Events;
using L2Market.Domain.Common;
using L2Market.Core.Services;

namespace L2Market.Core.Services
{
    /// <summary>
    /// Сервис отслеживания предметов на рынке
    /// </summary>
    public class TrackingService : IDisposable
    {
        private readonly IEventBus _eventBus;
        private readonly MarketManagerService _marketManager;
        private readonly ConcurrentDictionary<string, TrackingRule> _rules;
        private readonly Timer _priceCheckTimer;
        private readonly object _lock = new object();

        public TrackingService(IEventBus eventBus, MarketManagerService marketManager)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _marketManager = marketManager ?? throw new ArgumentNullException(nameof(marketManager));
            _rules = new ConcurrentDictionary<string, TrackingRule>();
            
            // Запускаем проверку цен каждые 10 секунд
            _priceCheckTimer = new Timer(CheckPricesAsync, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
            
            // Подписываемся на события обновления рынка
            _eventBus.Subscribe<PrivateStoreUpdatedEvent>(HandlePrivateStoreUpdated);
            _eventBus.Subscribe<CommissionUpdatedEvent>(HandleCommissionUpdated);
            _eventBus.Subscribe<WorldExchangeUpdatedEvent>(HandleWorldExchangeUpdated);
        }

        /// <summary>
        /// События
        /// </summary>
        public event EventHandler<ItemMatchFoundEventArgs>? ItemMatchFound;
        public event EventHandler<AutoBuyTriggeredEventArgs>? AutoBuyTriggered;

        /// <summary>
        /// Добавить правило отслеживания
        /// </summary>
        public async Task AddRuleAsync(TrackingRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            
            _rules.TryAdd(rule.Id, rule);
            await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[TrackingService] Added rule: {rule.Name}"));
        }

        /// <summary>
        /// Удалить правило отслеживания
        /// </summary>
        public async Task RemoveRuleAsync(string ruleId)
        {
            if (_rules.TryRemove(ruleId, out var rule))
            {
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[TrackingService] Removed rule: {rule.Name}"));
            }
        }

        /// <summary>
        /// Обновить правило отслеживания
        /// </summary>
        public async Task UpdateRuleAsync(TrackingRule rule)
        {
            if (rule == null) throw new ArgumentNullException(nameof(rule));
            
            _rules.AddOrUpdate(rule.Id, rule, (k, v) => rule);
            await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[TrackingService] Updated rule: {rule.Name}"));
        }

        /// <summary>
        /// Получить все правила
        /// </summary>
        public Task<List<TrackingRule>> GetRulesAsync()
        {
            return Task.FromResult(_rules.Values.ToList());
        }

        /// <summary>
        /// Получить активные правила
        /// </summary>
        public Task<List<TrackingRule>> GetActiveRulesAsync()
        {
            return Task.FromResult(_rules.Values.Where(r => r.IsEnabled).ToList());
        }

        /// <summary>
        /// Обработка обновления частных магазинов
        /// </summary>
        private async Task HandlePrivateStoreUpdated(PrivateStoreUpdatedEvent evt)
        {
            await CheckItemsAgainstRules(evt.Items.Select(MarketItemViewModel.FromPrivateStoreItem), MarketType.PrivateStore);
        }

        /// <summary>
        /// Обработка обновления комиссий
        /// </summary>
        private async Task HandleCommissionUpdated(CommissionUpdatedEvent evt)
        {
            await CheckItemsAgainstRules(evt.Items.Select(MarketItemViewModel.FromCommissionItem), MarketType.Commission);
        }

        /// <summary>
        /// Обработка обновления мирового обмена
        /// </summary>
        private async Task HandleWorldExchangeUpdated(WorldExchangeUpdatedEvent evt)
        {
            await CheckItemsAgainstRules(evt.Items.Select(MarketItemViewModel.FromWorldExchangeItem), MarketType.WorldExchange);
        }

        /// <summary>
        /// Проверка предметов против правил
        /// </summary>
        private Task CheckItemsAgainstRules(IEnumerable<MarketItemViewModel> items, MarketType marketType)
        {
            var activeRules = _rules.Values.Where(r => r.IsEnabled && (r.MarketType == marketType || r.MarketType == MarketType.All));
            
            foreach (var item in items)
            {
                foreach (var rule in activeRules)
                {
                    if (MatchesRule(item, rule))
                    {
                        rule.MatchesFound++;
                        rule.LastMatch = DateTime.UtcNow;
                        
                        var match = new ItemMatch
                        {
                            Rule = rule,
                            Item = item,
                            FoundAt = DateTime.UtcNow
                        };

                        // Отправляем уведомление
                        if (rule.HasNotifications && item.Price <= rule.NotificationPrice)
                        {
                            ItemMatchFound?.Invoke(this, new ItemMatchFoundEventArgs(match));
                        }

                        // Запускаем автовыкуп
                        if (rule.HasAutoBuy && item.Price <= rule.AutoBuyPrice)
                        {
                            AutoBuyTriggered?.Invoke(this, new AutoBuyTriggeredEventArgs(match));
                        }
                    }
                }
            }
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Проверка соответствия предмета правилу
        /// </summary>
        private bool MatchesRule(MarketItemViewModel item, TrackingRule rule)
        {
            // Проверяем ID предмета
            if (int.TryParse(item.ItemId, out var itemId) && itemId != rule.ItemId)
                return false;

            // Проверяем максимальную цену
            if (item.Price > rule.MaxPrice)
                return false;

            return true;
        }

        /// <summary>
        /// Периодическая проверка цен
        /// </summary>
        private void CheckPricesAsync(object? state)
        {
            try
            {
                // Здесь можно добавить дополнительную логику проверки
                // Например, анализ трендов цен
                _ = Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _ = _eventBus.PublishAsync(new LogMessageReceivedEvent($"[TrackingService] Error during price check: {ex.Message}"));
            }
        }

        public void Dispose()
        {
            _priceCheckTimer?.Dispose();
        }
    }

    /// <summary>
    /// Найденное совпадение предмета
    /// </summary>
    public class ItemMatch
    {
        public TrackingRule Rule { get; set; } = null!;
        public MarketItemViewModel Item { get; set; } = null!;
        public DateTime FoundAt { get; set; }
    }

    /// <summary>
    /// Аргументы события найденного совпадения
    /// </summary>
    public class ItemMatchFoundEventArgs : EventArgs
    {
        public ItemMatch Match { get; }

        public ItemMatchFoundEventArgs(ItemMatch match)
        {
            Match = match;
        }
    }

    /// <summary>
    /// Аргументы события автовыкупа
    /// </summary>
    public class AutoBuyTriggeredEventArgs : EventArgs
    {
        public ItemMatch Match { get; }

        public AutoBuyTriggeredEventArgs(ItemMatch match)
        {
            Match = match;
        }
    }
}
