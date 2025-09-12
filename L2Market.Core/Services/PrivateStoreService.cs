using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using L2Market.Domain.Entities.ExPrivateStoreSearchItemPacket;
using L2Market.Domain.Services;
using L2Market.Domain.Common;
using L2Market.Domain.Events;

namespace L2Market.Core.Services
{
    /// <summary>
    /// Сервис для отслеживания предметов в частных магазинах
    /// </summary>
    public class PrivateStoreService : IMarketService, IDisposable
    {
        private readonly IEventBus _eventBus;
        private readonly ConcurrentDictionary<string, PrivateStoreItem> _items;
        private readonly ConcurrentDictionary<int, HashSet<string>> _itemsByItemId;
        private readonly ConcurrentDictionary<string, DateTime> _itemLastSeen;
        private readonly Timer _cleanupTimer;
        private readonly object _lock = new object();
        
        // Время жизни предмета без обновлений (5 минут)
        private static readonly TimeSpan ItemLifetime = TimeSpan.FromMinutes(5);

        public PrivateStoreService(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _items = new ConcurrentDictionary<string, PrivateStoreItem>();
            _itemsByItemId = new ConcurrentDictionary<int, HashSet<string>>();
            _itemLastSeen = new ConcurrentDictionary<string, DateTime>();
            
            // Подписываемся на события обновления частных магазинов
            _eventBus.Subscribe<PrivateStoreUpdatedEvent>(HandlePrivateStoreUpdated);
            
            // Запускаем таймер очистки устаревших предметов (каждую минуту)
            _cleanupTimer = new Timer(CleanupExpiredItems, null, TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }

        /// <summary>
        /// Обработка обновления данных частного магазина
        /// </summary>
        private async Task HandlePrivateStoreUpdated(PrivateStoreUpdatedEvent evt)
        {
            try
            {
                // Группируем предметы по типу магазина для специальной обработки
                var groupedItems = evt.Items.GroupBy(item => item.StoreType).ToList();
                
                foreach (var group in groupedItems)
                {
                    var storeType = group.Key;
                    var items = group.ToList();
                    
                    if (storeType == 0x01) // Массовая продажа
                    {
                        await ProcessBulkSaleItemsAsync(items);
                    }
                    else
                    {
                        await UpdateItemsAsync(items);
                    }
                }
                
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[PrivateStoreService] Updated {evt.Items.Count} items (Bulk: {evt.Items.Count(i => i.StoreType == 0x01)}, Regular: {evt.Items.Count(i => i.StoreType != 0x01)})"));
            }
            catch (Exception ex)
            {
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[PrivateStoreService] Error updating items: {ex.Message}"));
            }
        }

        /// <summary>
        /// Обработка предметов массовой продажи
        /// </summary>
        private async Task ProcessBulkSaleItemsAsync(IReadOnlyList<PrivateStoreItem> bulkItems)
        {
            var currentTime = DateTime.UtcNow;
            
            // Группируем предметы по продавцу для массовых продаж
            var groupedByVendor = bulkItems.GroupBy(item => item.VendorObjectId).ToList();
            
            foreach (var vendorGroup in groupedByVendor)
            {
                var vendorObjectId = vendorGroup.Key;
                var vendorItems = vendorGroup.ToList();
                
                // Удаляем все старые предметы этого продавца (массовые продажи)
                var oldKeys = _items
                    .Where(kvp => GetVendorObjectIdFromKey(kvp.Key) == vendorObjectId && 
                                 kvp.Value.StoreType == 0x01)
                    .Select(kvp => kvp.Key)
                    .ToList();
                
                foreach (var key in oldKeys)
                {
                    if (_items.TryRemove(key, out var removedItem))
                    {
                        // Удаляем из индекса по ID предмета
                        if (_itemsByItemId.TryGetValue(removedItem.ItemInfo.ItemId, out var itemKeys))
                        {
                            itemKeys.Remove(key);
                            if (itemKeys.Count == 0)
                            {
                                _itemsByItemId.TryRemove(removedItem.ItemInfo.ItemId, out _);
                            }
                        }
                    }
                }
                
                // Добавляем новые предметы массовой продажи
                foreach (var item in vendorItems)
                {
                    var key = GenerateItemKey(item);
                    _items[key] = item;
                    _itemLastSeen[key] = currentTime;
                    
                    // Добавляем в индекс по ID предмета
                    _itemsByItemId.GetOrAdd(item.ItemInfo.ItemId, _ => new HashSet<string>()).Add(key);
                }
                
                await _eventBus.PublishAsync(new LogMessageReceivedEvent(
                    $"[PrivateStoreService] Processed bulk sale from vendor {vendorItems.First().VendorName}: {vendorItems.Count} items"));
            }
        }

        /// <summary>
        /// Обновление предметов в магазине
        /// </summary>
        private Task UpdateItemsAsync(IReadOnlyList<PrivateStoreItem> newItems)
        {
            var currentTime = DateTime.UtcNow;
            
            // Создаем словарь новых предметов для быстрого поиска
            var newItemsDict = newItems.ToDictionary(GenerateItemKey, item => item);
            
            // Получаем все существующие ключи для вендоров из нового пакета
            var vendorObjectIds = newItems.Select(i => i.VendorObjectId).Distinct().ToHashSet();
            var existingKeysForVendors = _items
                .Where(kvp => vendorObjectIds.Contains(GetVendorObjectIdFromKey(kvp.Key)))
                .Select(kvp => kvp.Key)
                .ToHashSet();
            
            // Удаляем предметы, которых больше нет в новом пакете
            var keysToRemove = existingKeysForVendors.Except(newItemsDict.Keys).ToList();
            System.Diagnostics.Debug.WriteLine($"[PrivateStoreService] Removing {keysToRemove.Count} items, adding {newItems.Count} items");
            
            foreach (var key in keysToRemove)
            {
                if (_items.TryRemove(key, out var removedItem))
                {
                    // Удаляем из индекса по ID предмета
                    if (_itemsByItemId.TryGetValue(removedItem.ItemInfo.ItemId, out var itemKeys))
                    {
                        itemKeys.Remove(key);
                        if (itemKeys.Count == 0)
                        {
                            _itemsByItemId.TryRemove(removedItem.ItemInfo.ItemId, out _);
                        }
                    }
                }
            }
            
            // Добавляем/обновляем новые предметы
            foreach (var item in newItems)
            {
                var key = GenerateItemKey(item);
                
                _items.AddOrUpdate(key, item, (k, v) => item);
                _itemLastSeen.AddOrUpdate(key, currentTime, (k, v) => currentTime);
                
                // Обновляем индекс по ID предмета
                _itemsByItemId.AddOrUpdate(
                    item.ItemInfo.ItemId,
                    new HashSet<string> { key },
                    (k, v) =>
                    {
                        v.Add(key);
                        return v;
                    });
            }
            
            System.Diagnostics.Debug.WriteLine($"[PrivateStoreService] Total items after update: {_items.Count}");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Извлечь VendorObjectId из ключа
        /// </summary>
        private int GetVendorObjectIdFromKey(string key)
        {
            var parts = key.Split('_');
            return parts.Length > 0 && int.TryParse(parts[0], out var vendorId) ? vendorId : -1;
        }

        /// <summary>
        /// Генерация уникального ключа для предмета
        /// </summary>
        private string GenerateItemKey(PrivateStoreItem item)
        {
            return $"{item.VendorObjectId}_{item.ItemInfo.ObjectId}_{item.ItemInfo.ItemId}";
        }

        /// <summary>
        /// Очистка устаревших предметов
        /// </summary>
        private async void CleanupExpiredItems(object? state)
        {
            try
            {
                var currentTime = DateTime.UtcNow;
                var expiredKeys = new List<string>();

                // Находим устаревшие предметы
                foreach (var kvp in _itemLastSeen)
                {
                    if (currentTime - kvp.Value > ItemLifetime)
                    {
                        expiredKeys.Add(kvp.Key);
                    }
                }

                // Удаляем устаревшие предметы
                var removedCount = 0;
                foreach (var key in expiredKeys)
                {
                    if (_items.TryRemove(key, out var removedItem))
                    {
                        _itemLastSeen.TryRemove(key, out _);
                        removedCount++;

                        // Удаляем из индекса по ID предмета
                        if (_itemsByItemId.TryGetValue(removedItem.ItemInfo.ItemId, out var itemKeys))
                        {
                            itemKeys.Remove(key);
                            if (itemKeys.Count == 0)
                            {
                                _itemsByItemId.TryRemove(removedItem.ItemInfo.ItemId, out _);
                            }
                        }
                    }
                }

                if (removedCount > 0)
                {
                    await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[PrivateStoreService] Removed {removedCount} expired items"));
                    // Публикуем событие об обновлении для UI
                    await _eventBus.PublishAsync(new PrivateStoreUpdatedEvent(new List<PrivateStoreItem>()));
                }
            }
            catch (Exception ex)
            {
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[PrivateStoreService] Error during cleanup: {ex.Message}"));
            }
        }

        public async Task<IEnumerable<object>> GetAllItemsAsync()
        {
            return await Task.FromResult(_items.Values.Cast<object>());
        }

        public async Task<IEnumerable<object>> GetItemsByItemIdAsync(int itemId)
        {
            if (_itemsByItemId.TryGetValue(itemId, out var itemKeys))
            {
                var items = itemKeys
                    .Where(key => _items.TryGetValue(key, out _))
                    .Select(key => _items[key])
                    .Cast<object>();
                
                return await Task.FromResult(items);
            }
            
            return await Task.FromResult(Enumerable.Empty<object>());
        }

        public async Task<IEnumerable<object>> GetItemsByPriceRangeAsync(long minPrice, long maxPrice)
        {
            var items = _items.Values
                .Where(item => item.Price >= minPrice && item.Price <= maxPrice)
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
        /// Освобождение ресурсов
        /// </summary>
        public void Dispose()
        {
            _cleanupTimer?.Dispose();
        }

        /// <summary>
        /// Получить предметы по имени продавца
        /// </summary>
        public async Task<IEnumerable<PrivateStoreItem>> GetItemsByVendorAsync(string vendorName)
        {
            var items = _items.Values
                .Where(item => string.Equals(item.VendorName, vendorName, StringComparison.OrdinalIgnoreCase));
            
            return await Task.FromResult(items);
        }

        /// <summary>
        /// Получить предметы по типу магазина
        /// </summary>
        public async Task<IEnumerable<PrivateStoreItem>> GetItemsByStoreTypeAsync(int storeType)
        {
            var items = _items.Values
                .Where(item => item.StoreType == storeType);
            
            return await Task.FromResult(items);
        }

        /// <summary>
        /// Получить предметы по типу магазина (enum)
        /// </summary>
        public async Task<IEnumerable<PrivateStoreItem>> GetItemsByStoreTypeAsync(StoreType storeType)
        {
            var items = _items.Values
                .Where(item => item.StoreType == (int)storeType);
            
            return await Task.FromResult(items);
        }

        /// <summary>
        /// Получить предметы по типу предмета
        /// </summary>
        public async Task<IEnumerable<PrivateStoreItem>> GetItemsByItemTypeAsync(StoreItemType itemType)
        {
            var items = _items.Values
                .Where(item => item.ItemInfo.ItemType2 == (int)itemType);
            
            return await Task.FromResult(items);
        }

        /// <summary>
        /// Получить предметы по подтипу предмета
        /// </summary>
        public async Task<IEnumerable<PrivateStoreItem>> GetItemsBySubItemTypeAsync(StoreSubItemType subItemType)
        {
            var items = _items.Values
                .Where(item => item.ItemInfo.CustomType1 == (int)subItemType);
            
            return await Task.FromResult(items);
        }

        /// <summary>
        /// Получить предметы по слоту тела
        /// </summary>
        public async Task<IEnumerable<PrivateStoreItem>> GetItemsByBodyPartAsync(BodyPart bodyPart)
        {
            var items = _items.Values
                .Where(item => (item.ItemInfo.BodyPart & (long)bodyPart) != 0);
            
            return await Task.FromResult(items);
        }

        /// <summary>
        /// Получить предметы с аугментацией
        /// </summary>
        public async Task<IEnumerable<PrivateStoreItem>> GetItemsWithAugmentationAsync()
        {
            var items = _items.Values
                .Where(item => item.ItemInfo.Augmentation != null && 
                              (item.ItemInfo.Augmentation.ContainsKey("option1") || 
                               item.ItemInfo.Augmentation.ContainsKey("option2")));
            
            return await Task.FromResult(items);
        }

        /// <summary>
        /// Получить предметы с элементарными атрибутами
        /// </summary>
        public async Task<IEnumerable<PrivateStoreItem>> GetItemsWithElementalAttributesAsync()
        {
            var items = _items.Values
                .Where(item => item.ItemInfo.ElementalAttrs != null && 
                              item.ItemInfo.ElementalAttrs.Any());
            
            return await Task.FromResult(items);
        }

        /// <summary>
        /// Получить предметы по уровню зачарования
        /// </summary>
        public async Task<IEnumerable<PrivateStoreItem>> GetItemsByEnchantLevelAsync(int minEnchant, int maxEnchant)
        {
            var items = _items.Values
                .Where(item => item.ItemInfo.EnchantLevel >= minEnchant && 
                              item.ItemInfo.EnchantLevel <= maxEnchant);
            
            return await Task.FromResult(items);
        }

        /// <summary>
        /// Получить предметы с soul crystal опциями
        /// </summary>
        public async Task<IEnumerable<PrivateStoreItem>> GetItemsWithSoulCrystalAsync()
        {
            var items = _items.Values
                .Where(item => item.ItemInfo.SoulCrystalOptions != null && 
                              item.ItemInfo.SoulCrystalOptions.Any());
            
            return await Task.FromResult(items);
        }

        /// <summary>
        /// Получить предметы с visual ID
        /// </summary>
        public async Task<IEnumerable<PrivateStoreItem>> GetItemsWithVisualIdAsync()
        {
            var items = _items.Values
                .Where(item => item.ItemInfo.VisualId.HasValue);
            
            return await Task.FromResult(items);
        }

        /// <summary>
        /// Получить предметы по элементарному атрибуту
        /// </summary>
        public async Task<IEnumerable<PrivateStoreItem>> GetItemsByElementalAttributeAsync(int attackType)
        {
            var items = _items.Values
                .Where(item => item.ItemInfo.ElementalAttrs != null && 
                              item.ItemInfo.ElementalAttrs.ContainsKey("attackType") &&
                              item.ItemInfo.ElementalAttrs["attackType"] == attackType);
            
            return await Task.FromResult(items);
        }

        /// <summary>
        /// Получить статистику по предметам
        /// </summary>
        public async Task<PrivateStoreStatistics> GetStatisticsAsync()
        {
            var items = _items.Values.ToList();
            
            var stats = new PrivateStoreStatistics
            {
                TotalItems = items.Count,
                UniqueVendors = items.Select(i => i.VendorName).Distinct().Count(),
                UniqueItemTypes = items.Select(i => i.ItemInfo.ItemId).Distinct().Count(),
                AveragePrice = items.Any() ? (long)items.Average(i => i.Price) : 0,
                MinPrice = items.Any() ? items.Min(i => i.Price) : 0,
                MaxPrice = items.Any() ? items.Max(i => i.Price) : 0,
                SellStores = items.Count(i => i.StoreType == (int)StoreType.Sell),
                BuyStores = items.Count(i => i.StoreType == (int)StoreType.Buy),
                EquipmentItems = items.Count(i => i.ItemInfo.ItemType2 == (int)StoreItemType.Equipment),
                EnhancementItems = items.Count(i => i.ItemInfo.ItemType2 == (int)StoreItemType.EnhancementOrExping),
                GroceryItems = items.Count(i => i.ItemInfo.ItemType2 == (int)StoreItemType.GroceryOrCollectionMisc),
                WithAugmentation = items.Count(i => i.ItemInfo.Augmentation != null && i.ItemInfo.Augmentation.Any()),
                WithElementalAttrs = items.Count(i => i.ItemInfo.ElementalAttrs != null && i.ItemInfo.ElementalAttrs.Any()),
                WithSoulCrystal = items.Count(i => i.ItemInfo.SoulCrystalOptions != null && i.ItemInfo.SoulCrystalOptions.Any()),
                WithVisualId = items.Count(i => i.ItemInfo.VisualId.HasValue)
            };
            
            return await Task.FromResult(stats);
        }
    }

    /// <summary>
    /// Статистика частных магазинов
    /// </summary>
    public class PrivateStoreStatistics
    {
        public int TotalItems { get; set; }
        public int UniqueVendors { get; set; }
        public int UniqueItemTypes { get; set; }
        public long AveragePrice { get; set; }
        public long MinPrice { get; set; }
        public long MaxPrice { get; set; }
        public int SellStores { get; set; }
        public int BuyStores { get; set; }
        public int EquipmentItems { get; set; }
        public int EnhancementItems { get; set; }
        public int GroceryItems { get; set; }
        public int WithAugmentation { get; set; }
        public int WithElementalAttrs { get; set; }
        public int WithSoulCrystal { get; set; }
        public int WithVisualId { get; set; }
    }

    /// <summary>
    /// Событие обновления частного магазина
    /// </summary>
    public class PrivateStoreUpdatedEvent
    {
        public IReadOnlyList<PrivateStoreItem> Items { get; }
        public DateTime Timestamp { get; }

        public PrivateStoreUpdatedEvent(IReadOnlyList<PrivateStoreItem> items)
        {
            Items = items ?? throw new ArgumentNullException(nameof(items));
            Timestamp = DateTime.UtcNow;
        }
    }
}
