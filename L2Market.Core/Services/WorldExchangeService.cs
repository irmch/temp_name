using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using L2Market.Domain.Entities.WorldExchangeItemListPacket;
using L2Market.Domain.Services;
using L2Market.Domain.Common;
using L2Market.Domain.Events;

namespace L2Market.Core.Services
{
    /// <summary>
    /// Сервис для отслеживания предметов в мировом обмене
    /// </summary>
    public class WorldExchangeService : IMarketService, IDisposable
    {
        private readonly IEventBus _eventBus;
        private readonly ConcurrentDictionary<ulong, WorldExchangeItemInfo> _items;
        private readonly ConcurrentDictionary<int, HashSet<ulong>> _itemsByItemId;
        private readonly ConcurrentDictionary<int, HashSet<ulong>> _itemsByCategory;
        private readonly ConcurrentDictionary<ulong, DateTime> _itemLastSeen;
        private readonly Timer _cleanupTimer;
        private readonly object _lock = new object();
        
        // Время жизни предмета без обновлений (15 минут для мирового обмена)
        private static readonly TimeSpan ItemLifetime = TimeSpan.FromMinutes(15);

        public WorldExchangeService(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _items = new ConcurrentDictionary<ulong, WorldExchangeItemInfo>();
            _itemsByItemId = new ConcurrentDictionary<int, HashSet<ulong>>();
            _itemsByCategory = new ConcurrentDictionary<int, HashSet<ulong>>();
            _itemLastSeen = new ConcurrentDictionary<ulong, DateTime>();
            
            // Подписываемся на события обновления мирового обмена
            _eventBus.Subscribe<WorldExchangeUpdatedEvent>(HandleWorldExchangeUpdated);
            
            // Запускаем таймер очистки устаревших предметов (каждые 3 минуты)
            _cleanupTimer = new Timer(CleanupExpiredItems, null, TimeSpan.FromMinutes(3), TimeSpan.FromMinutes(3));
        }

        /// <summary>
        /// Обработка обновления данных мирового обмена
        /// </summary>
        private async Task HandleWorldExchangeUpdated(WorldExchangeUpdatedEvent evt)
        {
            try
            {
                await UpdateItemsAsync(evt.Items, evt.Category);
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[WorldExchangeService] Updated {evt.Items.Count} items"));
            }
            catch (Exception ex)
            {
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[WorldExchangeService] Error updating items: {ex.Message}"));
            }
        }

        /// <summary>
        /// Обновление предметов в мировом обмене
        /// </summary>
        private Task UpdateItemsAsync(IReadOnlyList<WorldExchangeItemInfo> newItems, int? category = null)
        {
            var currentTime = DateTime.UtcNow;
            
            System.Diagnostics.Debug.WriteLine($"[WorldExchangeService] Adding/updating {newItems.Count} items");
            
            // Добавляем/обновляем новые предметы
            foreach (var item in newItems)
            {
                _items.AddOrUpdate(item.WorldExchangeId, item, (k, v) => item);
                _itemLastSeen.AddOrUpdate(item.WorldExchangeId, currentTime, (k, v) => currentTime);
                
                // Обновляем индекс по ID предмета
                _itemsByItemId.AddOrUpdate(
                    item.ItemId,
                    new HashSet<ulong> { item.WorldExchangeId },
                    (k, v) =>
                    {
                        v.Add(item.WorldExchangeId);
                        return v;
                    });

                // Обновляем индекс по категории (если есть)
                if (category.HasValue)
                {
                    _itemsByCategory.AddOrUpdate(
                        category.Value,
                        new HashSet<ulong> { item.WorldExchangeId },
                        (k, v) =>
                        {
                            v.Add(item.WorldExchangeId);
                            return v;
                        });
                }
            }
            
            System.Diagnostics.Debug.WriteLine($"[WorldExchangeService] Total items after update: {_items.Count}");
            return Task.CompletedTask;
        }

        public async Task<IEnumerable<object>> GetAllItemsAsync()
        {
            return await Task.FromResult(_items.Values.Cast<object>());
        }

        public async Task<IEnumerable<object>> GetItemsByItemIdAsync(int itemId)
        {
            if (_itemsByItemId.TryGetValue(itemId, out var itemIds))
            {
                var items = itemIds
                    .Where(id => _items.TryGetValue(id, out _))
                    .Select(id => _items[id])
                    .Cast<object>();
                
                return await Task.FromResult(items);
            }
            
            return await Task.FromResult(Enumerable.Empty<object>());
        }

        public async Task<IEnumerable<object>> GetItemsByPriceRangeAsync(long minPrice, long maxPrice)
        {
            var items = _items.Values
                .Where(item => (long)item.Price >= minPrice && (long)item.Price <= maxPrice)
                .Cast<object>();
            
            return await Task.FromResult(items);
        }

        public async Task<int> GetItemsCountAsync()
        {
            return await Task.FromResult(_items.Count);
        }

        public async Task ClearAsync()
        {
            _items.Clear();
            _itemsByItemId.Clear();
            _itemsByCategory.Clear();
            _itemLastSeen.Clear();
            await Task.CompletedTask;
        }

        /// <summary>
        /// Очистка устаревших предметов
        /// </summary>
        private async void CleanupExpiredItems(object? state)
        {
            try
            {
                var currentTime = DateTime.UtcNow;
                var expiredIds = new List<ulong>();

                // Находим устаревшие предметы
                foreach (var kvp in _itemLastSeen)
                {
                    if (currentTime - kvp.Value > ItemLifetime)
                    {
                        expiredIds.Add(kvp.Key);
                    }
                }

                // Удаляем устаревшие предметы
                var removedCount = 0;
                foreach (var id in expiredIds)
                {
                    if (_items.TryRemove(id, out var removedItem))
                    {
                        _itemLastSeen.TryRemove(id, out _);
                        removedCount++;

                        // Удаляем из индекса по ID предмета
                        if (_itemsByItemId.TryGetValue(removedItem.ItemId, out var itemIds))
                        {
                            itemIds.Remove(id);
                            if (itemIds.Count == 0)
                            {
                                _itemsByItemId.TryRemove(removedItem.ItemId, out _);
                            }
                        }

                        // Удаляем из индекса по категории
                        foreach (var category in _itemsByCategory.Keys.ToList())
                        {
                            if (_itemsByCategory.TryGetValue(category, out var categoryItems))
                            {
                                categoryItems.Remove(id);
                                if (categoryItems.Count == 0)
                                {
                                    _itemsByCategory.TryRemove(category, out _);
                                }
                            }
                        }
                    }
                }

                if (removedCount > 0)
                {
                    await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[WorldExchangeService] Removed {removedCount} expired items"));
                    // Публикуем событие об обновлении для UI
                    await _eventBus.PublishAsync(new WorldExchangeUpdatedEvent(new List<WorldExchangeItemInfo>(), 0));
                }
            }
            catch (Exception ex)
            {
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[WorldExchangeService] Error during cleanup: {ex.Message}"));
            }
        }

        /// <summary>
        /// Освобождение ресурсов
        /// </summary>
        public void Dispose()
        {
            _cleanupTimer?.Dispose();
        }

        /// <summary>
        /// Получить предметы по категории
        /// </summary>
        public async Task<IEnumerable<WorldExchangeItemInfo>> GetItemsByCategoryAsync(int category)
        {
            if (_itemsByCategory.TryGetValue(category, out var itemIds))
            {
                var items = itemIds
                    .Where(id => _items.TryGetValue(id, out _))
                    .Select(id => _items[id]);
                
                return await Task.FromResult(items);
            }
            
            return await Task.FromResult(Enumerable.Empty<WorldExchangeItemInfo>());
        }

        /// <summary>
        /// Получить предметы по уровню зачарования
        /// </summary>
        public async Task<IEnumerable<WorldExchangeItemInfo>> GetItemsByEnchantLevelAsync(int minEnchant, int maxEnchant)
        {
            var items = _items.Values
                .Where(item => item.EnchantLevel >= minEnchant && item.EnchantLevel <= maxEnchant);
            
            return await Task.FromResult(items);
        }

        /// <summary>
        /// Получить предметы по времени окончания
        /// </summary>
        public async Task<IEnumerable<WorldExchangeItemInfo>> GetItemsByEndTimeAsync(int currentTime, int maxTimeRemaining)
        {
            var items = _items.Values
                .Where(item => item.EndTime - currentTime <= maxTimeRemaining);
            
            return await Task.FromResult(items);
        }

        /// <summary>
        /// Получить предметы с аугментацией
        /// </summary>
        public async Task<IEnumerable<WorldExchangeItemInfo>> GetItemsWithAugmentationAsync()
        {
            var items = _items.Values
                .Where(item => item.AugmentationOption1 != 0 || item.AugmentationOption2 != 0);
            
            return await Task.FromResult(items);
        }

        /// <summary>
        /// Получить предметы с soul crystal опциями
        /// </summary>
        public async Task<IEnumerable<WorldExchangeItemInfo>> GetItemsWithSoulCrystalAsync()
        {
            var items = _items.Values
                .Where(item => item.SoulCrystalOption1 != 0 || item.SoulCrystalOption2 != 0 || item.SoulCrystalSpecialOption != 0);
            
            return await Task.FromResult(items);
        }

        /// <summary>
        /// Получить предметы по подтипу предмета
        /// </summary>
        public async Task<IEnumerable<WorldExchangeItemInfo>> GetItemsBySubTypeAsync(WorldExchangeItemSubType subType)
        {
            // В WorldExchangeItemInfo нет прямого поля subType, но можно определить по itemId
            // Это упрощенная реализация - в реальности нужна база данных соответствий itemId -> subType
            var items = _items.Values;
            
            return await Task.FromResult(items);
        }

        /// <summary>
        /// Получить предметы с blessed статусом
        /// </summary>
        public async Task<IEnumerable<WorldExchangeItemInfo>> GetBlessedItemsAsync()
        {
            var items = _items.Values
                .Where(item => item.IsBlessed != 0);
            
            return await Task.FromResult(items);
        }

        /// <summary>
        /// Получить предметы с атакующими атрибутами
        /// </summary>
        public async Task<IEnumerable<WorldExchangeItemInfo>> GetItemsWithAttackAttributesAsync()
        {
            var items = _items.Values
                .Where(item => item.AttackAttributeType != 0 || item.AttackAttributeValue != 0);
            
            return await Task.FromResult(items);
        }

        /// <summary>
        /// Получить предметы с защитными атрибутами
        /// </summary>
        public async Task<IEnumerable<WorldExchangeItemInfo>> GetItemsWithDefenceAttributesAsync()
        {
            var items = _items.Values
                .Where(item => item.DefenceFire != 0 || item.DefenceWater != 0 || 
                              item.DefenceWind != 0 || item.DefenceEarth != 0 || 
                              item.DefenceHoly != 0 || item.DefenceDark != 0);
            
            return await Task.FromResult(items);
        }

        /// <summary>
        /// Получить предметы с visual ID
        /// </summary>
        public async Task<IEnumerable<WorldExchangeItemInfo>> GetItemsWithVisualIdAsync()
        {
            var items = _items.Values
                .Where(item => item.VisualId != 0);
            
            return await Task.FromResult(items);
        }

        /// <summary>
        /// Получить предметы по типу атаки
        /// </summary>
        public async Task<IEnumerable<WorldExchangeItemInfo>> GetItemsByAttackTypeAsync(ushort attackType)
        {
            var items = _items.Values
                .Where(item => item.AttackAttributeType == attackType);
            
            return await Task.FromResult(items);
        }

        /// <summary>
        /// Получить предметы по силе атаки
        /// </summary>
        public async Task<IEnumerable<WorldExchangeItemInfo>> GetItemsByAttackPowerAsync(ushort minPower, ushort maxPower)
        {
            var items = _items.Values
                .Where(item => item.AttackAttributeValue >= minPower && item.AttackAttributeValue <= maxPower);
            
            return await Task.FromResult(items);
        }

        /// <summary>
        /// Получить статистику по предметам
        /// </summary>
        public async Task<WorldExchangeStatistics> GetStatisticsAsync()
        {
            var items = _items.Values.ToList();
            var currentTime = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            
            var stats = new WorldExchangeStatistics
            {
                TotalItems = items.Count,
                UniqueItemTypes = items.Select(i => i.ItemId).Distinct().Count(),
                CategoriesCount = _itemsByCategory.Count,
                AveragePrice = items.Any() ? (long)items.Average(i => (long)i.Price) : 0,
                MinPrice = items.Any() ? (long)items.Min(i => i.Price) : 0,
                MaxPrice = items.Any() ? (long)items.Max(i => i.Price) : 0,
                ExpiringSoon = items.Count(i => i.EndTime - currentTime <= 3600), // 1 час
                WithAugmentation = items.Count(i => i.AugmentationOption1 != 0 || i.AugmentationOption2 != 0),
                WithSoulCrystal = items.Count(i => i.SoulCrystalOption1 != 0 || i.SoulCrystalOption2 != 0 || i.SoulCrystalSpecialOption != 0),
                BlessedItems = items.Count(i => i.IsBlessed != 0),
                WithAttackAttributes = items.Count(i => i.AttackAttributeType != 0 || i.AttackAttributeValue != 0),
                WithDefenceAttributes = items.Count(i => i.DefenceFire != 0 || i.DefenceWater != 0 || 
                                                      i.DefenceWind != 0 || i.DefenceEarth != 0 || 
                                                      i.DefenceHoly != 0 || i.DefenceDark != 0),
                WithVisualId = items.Count(i => i.VisualId != 0),
                HighEnchantItems = items.Count(i => i.EnchantLevel >= 10),
                AverageEnchantLevel = items.Any() ? (double)items.Average(i => i.EnchantLevel) : 0
            };
            
            return await Task.FromResult(stats);
        }
    }

    /// <summary>
    /// Статистика мирового обмена
    /// </summary>
    public class WorldExchangeStatistics
    {
        public int TotalItems { get; set; }
        public int UniqueItemTypes { get; set; }
        public int CategoriesCount { get; set; }
        public long AveragePrice { get; set; }
        public long MinPrice { get; set; }
        public long MaxPrice { get; set; }
        public int ExpiringSoon { get; set; }
        public int WithAugmentation { get; set; }
        public int WithSoulCrystal { get; set; }
        public int BlessedItems { get; set; }
        public int WithAttackAttributes { get; set; }
        public int WithDefenceAttributes { get; set; }
        public int WithVisualId { get; set; }
        public int HighEnchantItems { get; set; }
        public double AverageEnchantLevel { get; set; }
    }

    /// <summary>
    /// Событие обновления мирового обмена
    /// </summary>
    public class WorldExchangeUpdatedEvent
    {
        public IReadOnlyList<WorldExchangeItemInfo> Items { get; }
        public int? Category { get; }
        public DateTime Timestamp { get; }

        public WorldExchangeUpdatedEvent(IReadOnlyList<WorldExchangeItemInfo> items, int? category = null)
        {
            Items = items ?? throw new ArgumentNullException(nameof(items));
            Category = category;
            Timestamp = DateTime.UtcNow;
        }
    }
}
