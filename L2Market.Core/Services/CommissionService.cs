using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using L2Market.Domain.Entities.ExResponseCommissionListPacket;
using L2Market.Domain.Services;
using L2Market.Domain.Common;
using L2Market.Domain.Events;

namespace L2Market.Core.Services
{
    /// <summary>
    /// Сервис для отслеживания предметов в комиссионных магазинах
    /// </summary>
    public class CommissionService : IMarketService, IDisposable
    {
        private readonly ILocalEventBus _eventBus;
        private readonly uint _processId;
        private readonly ConcurrentDictionary<ulong, CommissionItem> _items;
        private readonly ConcurrentDictionary<int, HashSet<ulong>> _itemsByItemId;
        private readonly ConcurrentDictionary<ulong, DateTime> _itemLastSeen;
        private readonly Timer _cleanupTimer;
        private readonly object _lock = new object();
        
        // Время жизни предмета без обновлений (10 минут для комиссий)
        private static readonly TimeSpan ItemLifetime = TimeSpan.FromMinutes(10);

        public CommissionService(ILocalEventBus eventBus, uint processId = 0)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _processId = processId;
            _items = new ConcurrentDictionary<ulong, CommissionItem>();
            _itemsByItemId = new ConcurrentDictionary<int, HashSet<ulong>>();
            _itemLastSeen = new ConcurrentDictionary<ulong, DateTime>();
            
            // Подписываемся на события обновления комиссионных магазинов
            _eventBus.Subscribe<CommissionUpdatedEvent>(HandleCommissionUpdated);
            
            // Запускаем таймер очистки устаревших предметов (каждые 2 минуты)
            _cleanupTimer = new Timer(CleanupExpiredItems, null, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(2));
        }

        /// <summary>
        /// Обработка обновления данных комиссионного магазина
        /// </summary>
        private async Task HandleCommissionUpdated(CommissionUpdatedEvent evt)
        {
            try
            {
                await UpdateItemsAsync(evt.Items);
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[CommissionService] Updated {evt.Items.Count} items"));
            }
            catch (Exception ex)
            {
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[CommissionService] Error updating items: {ex.Message}"));
            }
        }

        /// <summary>
        /// Обновление предметов в комиссионном магазине
        /// </summary>
        private async Task UpdateItemsAsync(IReadOnlyList<CommissionItem> newItems)
        {
            var currentTime = DateTime.UtcNow;
            
            await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[CommissionService] Adding/updating {newItems.Count} items"));
            
            // Добавляем/обновляем новые предметы
            foreach (var item in newItems)
            {
                // Логируем детали каждого предмета с особым вниманием к нулевым ценам
                if (item.PricePerUnit == 0)
                {
                    await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[CommissionService] ⚠️ ZERO PRICE: ID={item.CommissionId}, Price={item.PricePerUnit}, Count={item.ItemInfo.Count}, Seller='{item.SellerName}', Type={item.CommissionItemType}, Duration={item.DurationType}"));
                }
                else if (item.PricePerUnit >= 1000000000000) // Цены в триллионах
                {
                    var priceInTrillions = (double)item.PricePerUnit / 1000000000000;
                    await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[CommissionService] 💰 TRILLION PRICE: ID={item.CommissionId}, Price={priceInTrillions:F2}T, Count={item.ItemInfo.Count}, Seller='{item.SellerName}'"));
                }
                else if (item.PricePerUnit >= 1000000000) // Цены в миллиардах
                {
                    var priceInBillions = (double)item.PricePerUnit / 1000000000;
                    await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[CommissionService] 💰 BILLION PRICE: ID={item.CommissionId}, Price={priceInBillions:F2}B, Count={item.ItemInfo.Count}, Seller='{item.SellerName}'"));
                }
                else
                {
                    await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[CommissionService] Item: ID={item.CommissionId}, Price={item.PricePerUnit}, Count={item.ItemInfo.Count}, Seller='{item.SellerName}'"));
                }
                
                _items.AddOrUpdate(item.CommissionId, item, (k, v) => item);
                _itemLastSeen.AddOrUpdate(item.CommissionId, currentTime, (k, v) => currentTime);
                
                // Обновляем индекс по ID предмета
                _itemsByItemId.AddOrUpdate(
                    item.ItemInfo.ItemId,
                    new HashSet<ulong> { item.CommissionId },
                    (k, v) =>
                    {
                        v.Add(item.CommissionId);
                        return v;
                    });
            }
            
            await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[CommissionService] Total items after update: {_items.Count}"));
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
                .Where(item => (long)item.PricePerUnit >= minPrice && (long)item.PricePerUnit <= maxPrice)
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
                        if (_itemsByItemId.TryGetValue(removedItem.ItemInfo.ItemId, out var itemIds))
                        {
                            itemIds.Remove(id);
                            if (itemIds.Count == 0)
                            {
                                _itemsByItemId.TryRemove(removedItem.ItemInfo.ItemId, out _);
                            }
                        }
                    }
                }

                if (removedCount > 0)
                {
                    await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[CommissionService] Removed {removedCount} expired items"));
                    // Публикуем событие об обновлении для UI
                    await _eventBus.PublishAsync(new CommissionUpdatedEvent(new List<CommissionItem>(), _processId));
                }
            }
            catch (Exception ex)
            {
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[CommissionService] Error during cleanup: {ex.Message}"));
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
        /// Получить предметы по имени продавца
        /// </summary>
        public async Task<IEnumerable<CommissionItem>> GetItemsBySellerAsync(string sellerName)
        {
            var items = _items.Values
                .Where(item => !string.IsNullOrEmpty(item.SellerName) && 
                              string.Equals(item.SellerName, sellerName, StringComparison.OrdinalIgnoreCase));
            
            return await Task.FromResult(items);
        }

        /// <summary>
        /// Получить предметы по типу комиссии
        /// </summary>
        public async Task<IEnumerable<CommissionItem>> GetItemsByCommissionTypeAsync(int commissionType)
        {
            var items = _items.Values
                .Where(item => item.CommissionItemType == commissionType);
            
            return await Task.FromResult(items);
        }

        /// <summary>
        /// Получить предметы по типу ответа комиссии
        /// </summary>
        public async Task<IEnumerable<CommissionItem>> GetItemsByReplyTypeAsync(CommissionListReplyType replyType)
        {
            // Этот метод может быть полезен для фильтрации по типу ответа
            // В реальности все предметы в сервисе уже отфильтрованы по replyType
            return await Task.FromResult(_items.Values);
        }

        /// <summary>
        /// Получить предметы по типу длительности
        /// </summary>
        public async Task<IEnumerable<CommissionItem>> GetItemsByDurationTypeAsync(int durationType)
        {
            var items = _items.Values
                .Where(item => item.DurationType == durationType);
            
            return await Task.FromResult(items);
        }

        /// <summary>
        /// Получить предметы с аугментацией
        /// </summary>
        public async Task<IEnumerable<CommissionItem>> GetItemsWithAugmentationAsync()
        {
            var items = _items.Values
                .Where(item => item.ItemInfo.Augmentation != null && 
                              item.ItemInfo.Augmentation.Any());
            
            return await Task.FromResult(items);
        }

        /// <summary>
        /// Получить предметы с элементарными атрибутами
        /// </summary>
        public async Task<IEnumerable<CommissionItem>> GetItemsWithElementalAttributesAsync()
        {
            var items = _items.Values
                .Where(item => item.ItemInfo.ElementalAttrs != null && 
                              item.ItemInfo.ElementalAttrs.Any());
            
            return await Task.FromResult(items);
        }

        /// <summary>
        /// Получить предметы с soul crystal опциями
        /// </summary>
        public async Task<IEnumerable<CommissionItem>> GetItemsWithSoulCrystalAsync()
        {
            var items = _items.Values
                .Where(item => item.ItemInfo.SoulCrystalOptions != null && 
                              item.ItemInfo.SoulCrystalOptions.Any());
            
            return await Task.FromResult(items);
        }

        /// <summary>
        /// Получить предметы с visual ID
        /// </summary>
        public async Task<IEnumerable<CommissionItem>> GetItemsWithVisualIdAsync()
        {
            var items = _items.Values
                .Where(item => item.ItemInfo.VisualId.HasValue);
            
            return await Task.FromResult(items);
        }

        /// <summary>
        /// Получить предметы по уровню зачарования
        /// </summary>
        public async Task<IEnumerable<CommissionItem>> GetItemsByEnchantLevelAsync(int minEnchant, int maxEnchant)
        {
            var items = _items.Values
                .Where(item => item.ItemInfo.EnchantLevel >= minEnchant && 
                              item.ItemInfo.EnchantLevel <= maxEnchant);
            
            return await Task.FromResult(items);
        }

        /// <summary>
        /// Получить предметы по времени окончания
        /// </summary>
        public async Task<IEnumerable<CommissionItem>> GetItemsByEndTimeAsync(int currentTime, int maxTimeRemaining)
        {
            var items = _items.Values
                .Where(item => item.EndTime - currentTime <= maxTimeRemaining);
            
            return await Task.FromResult(items);
        }

        /// <summary>
        /// Получить статистику по предметам
        /// </summary>
        public async Task<CommissionStatistics> GetStatisticsAsync()
        {
            var items = _items.Values.ToList();
            var currentTime = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            
            var stats = new CommissionStatistics
            {
                TotalItems = items.Count,
                UniqueSellers = items.Where(i => !string.IsNullOrEmpty(i.SellerName))
                                   .Select(i => i.SellerName)
                                   .Distinct()
                                   .Count(),
                UniqueItemTypes = items.Select(i => i.ItemInfo.ItemId).Distinct().Count(),
                AveragePrice = items.Any() ? (long)items.Average(i => (decimal)i.PricePerUnit) : 0,
                MinPrice = items.Any() ? (long)items.Min(i => i.PricePerUnit) : 0,
                MaxPrice = items.Any() ? (long)items.Max(i => i.PricePerUnit) : 0,
                ExpiringSoon = items.Count(i => i.EndTime - currentTime <= 3600), // 1 час
                WithAugmentation = items.Count(i => i.ItemInfo.Augmentation != null && i.ItemInfo.Augmentation.Any()),
                WithElementalAttrs = items.Count(i => i.ItemInfo.ElementalAttrs != null && i.ItemInfo.ElementalAttrs.Any()),
                WithSoulCrystal = items.Count(i => i.ItemInfo.SoulCrystalOptions != null && i.ItemInfo.SoulCrystalOptions.Any()),
                WithVisualId = items.Count(i => i.ItemInfo.VisualId.HasValue),
                HighEnchantItems = items.Count(i => i.ItemInfo.EnchantLevel >= 10),
                BlessedItems = items.Count(i => i.ItemInfo.Blessed == true)
            };
            
            return await Task.FromResult(stats);
        }
    }

    /// <summary>
    /// Статистика комиссионных магазинов
    /// </summary>
    public class CommissionStatistics
    {
        public int TotalItems { get; set; }
        public int UniqueSellers { get; set; }
        public int UniqueItemTypes { get; set; }
        public long AveragePrice { get; set; }
        public long MinPrice { get; set; }
        public long MaxPrice { get; set; }
        public int ExpiringSoon { get; set; }
        public int WithAugmentation { get; set; }
        public int WithElementalAttrs { get; set; }
        public int WithSoulCrystal { get; set; }
        public int WithVisualId { get; set; }
        public int HighEnchantItems { get; set; }
        public int BlessedItems { get; set; }
    }

}
