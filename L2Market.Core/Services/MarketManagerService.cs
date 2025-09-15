using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using L2Market.Domain.Services;
using L2Market.Domain.Common;
using L2Market.Domain.Events;

namespace L2Market.Core.Services
{
    /// <summary>
    /// Менеджер для работы со всеми типами магазинов
    /// </summary>
    public class MarketManagerService
    {
        private readonly PrivateStoreService _privateStoreService;
        private readonly CommissionService _commissionService;
        private readonly WorldExchangeService _worldExchangeService;
        private readonly ILocalEventBus _eventBus;

        public MarketManagerService(
            PrivateStoreService privateStoreService,
            CommissionService commissionService,
            WorldExchangeService worldExchangeService,
            ILocalEventBus eventBus)
        {
            _privateStoreService = privateStoreService ?? throw new ArgumentNullException(nameof(privateStoreService));
            _commissionService = commissionService ?? throw new ArgumentNullException(nameof(commissionService));
            _worldExchangeService = worldExchangeService ?? throw new ArgumentNullException(nameof(worldExchangeService));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        }

        /// <summary>
        /// Получить сервис частных магазинов
        /// </summary>
        public PrivateStoreService PrivateStores => _privateStoreService;

        /// <summary>
        /// Получить сервис комиссионных магазинов
        /// </summary>
        public CommissionService Commissions => _commissionService;

        /// <summary>
        /// Получить сервис мирового обмена
        /// </summary>
        public WorldExchangeService WorldExchange => _worldExchangeService;

        /// <summary>
        /// Получить общую статистику по всем магазинам
        /// </summary>
        public async Task<MarketOverviewStatistics> GetOverviewStatisticsAsync()
        {
            var privateStoreStats = await _privateStoreService.GetStatisticsAsync();
            var commissionStats = await _commissionService.GetStatisticsAsync();
            var worldExchangeStats = await _worldExchangeService.GetStatisticsAsync();

            return new MarketOverviewStatistics
            {
                PrivateStores = privateStoreStats,
                Commissions = commissionStats,
                WorldExchange = worldExchangeStats,
                TotalItems = privateStoreStats.TotalItems + commissionStats.TotalItems + worldExchangeStats.TotalItems,
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Очистить все данные во всех магазинах
        /// </summary>
        public async Task ClearAllAsync()
        {
            await _privateStoreService.ClearAsync();
            await _commissionService.ClearAsync();
            await _worldExchangeService.ClearAsync();
            
            await _eventBus.PublishAsync(new LogMessageReceivedEvent("[MarketManager] All market data cleared"));
        }

        /// <summary>
        /// Получить предметы по ID из всех магазинов
        /// </summary>
        public async Task<MarketSearchResult> SearchItemsByItemIdAsync(int itemId)
        {
            var privateStoreItems = await _privateStoreService.GetItemsByItemIdAsync(itemId);
            var commissionItems = await _commissionService.GetItemsByItemIdAsync(itemId);
            var worldExchangeItems = await _worldExchangeService.GetItemsByItemIdAsync(itemId);

            return new MarketSearchResult
            {
                ItemId = itemId,
                PrivateStoreItems = privateStoreItems,
                CommissionItems = commissionItems,
                WorldExchangeItems = worldExchangeItems,
                TotalCount = privateStoreItems.Count() + commissionItems.Count() + worldExchangeItems.Count(),
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Получить предметы по диапазону цен из всех магазинов
        /// </summary>
        public async Task<MarketSearchResult> SearchItemsByPriceRangeAsync(long minPrice, long maxPrice)
        {
            var privateStoreItems = await _privateStoreService.GetItemsByPriceRangeAsync(minPrice, maxPrice);
            var commissionItems = await _commissionService.GetItemsByPriceRangeAsync(minPrice, maxPrice);
            var worldExchangeItems = await _worldExchangeService.GetItemsByPriceRangeAsync(minPrice, maxPrice);

            return new MarketSearchResult
            {
                ItemId = 0, // Не применимо для поиска по цене
                PrivateStoreItems = privateStoreItems,
                CommissionItems = commissionItems,
                WorldExchangeItems = worldExchangeItems,
                TotalCount = privateStoreItems.Count() + commissionItems.Count() + worldExchangeItems.Count(),
                Timestamp = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// Общая статистика по всем магазинам
    /// </summary>
    public class MarketOverviewStatistics
    {
        public PrivateStoreStatistics PrivateStores { get; set; } = new();
        public CommissionStatistics Commissions { get; set; } = new();
        public WorldExchangeStatistics WorldExchange { get; set; } = new();
        public int TotalItems { get; set; }
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Результат поиска по всем магазинам
    /// </summary>
    public class MarketSearchResult
    {
        public int ItemId { get; set; }
        public IEnumerable<object> PrivateStoreItems { get; set; } = new List<object>();
        public IEnumerable<object> CommissionItems { get; set; } = new List<object>();
        public IEnumerable<object> WorldExchangeItems { get; set; } = new List<object>();
        public int TotalCount { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
