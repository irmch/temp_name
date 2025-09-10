using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace L2Market.Domain.Services
{
    /// <summary>
    /// Базовый интерфейс для сервисов отслеживания рынка
    /// </summary>
    public interface IMarketService
    {
        /// <summary>
        /// Получить все активные предметы
        /// </summary>
        Task<IEnumerable<object>> GetAllItemsAsync();
        
        /// <summary>
        /// Получить предметы по ID предмета
        /// </summary>
        Task<IEnumerable<object>> GetItemsByItemIdAsync(int itemId);
        
        /// <summary>
        /// Получить предметы по цене (диапазон)
        /// </summary>
        Task<IEnumerable<object>> GetItemsByPriceRangeAsync(long minPrice, long maxPrice);
        
        /// <summary>
        /// Получить количество активных предметов
        /// </summary>
        Task<int> GetItemsCountAsync();
        
        /// <summary>
        /// Очистить все данные
        /// </summary>
        Task ClearAsync();
    }
}
