using System;
using System.Collections.Generic;
using System.Linq;
using L2Market.Domain.Entities.ExPrivateStoreSearchItemPacket;
using L2Market.Domain.Entities.ExResponseCommissionListPacket;
using L2Market.Domain.Entities.WorldExchangeItemListPacket;

namespace L2Market.Core.Services
{
    /// <summary>
    /// Утилитный класс для декодирования и форматирования данных рынка
    /// </summary>
    public static class MarketDecoders
    {
        /// <summary>
        /// Декодирование типов магазинов
        /// </summary>
        public static string DecodeStoreType(int storeType)
        {
            return storeType switch
            {
                0 => "Продажа",
                1 => "Покупка", 
                2 => "Пакетная продажа",
                _ => $"Неизвестный ({storeType})"
            };
        }

        /// <summary>
        /// Декодирование типов предметов
        /// </summary>
        public static string DecodeItemType(int itemType)
        {
            return itemType switch
            {
                0 => "Оружие",
                1 => "Щит/Броня",
                2 => "Аксессуар",
                3 => "Квест",
                4 => "Деньги",
                5 => "Прочее",
                _ => $"Неизвестный ({itemType})"
            };
        }

        /// <summary>
        /// Декодирование подтипов предметов
        /// </summary>
        public static string DecodeSubItemType(int subItemType)
        {
            return subItemType switch
            {
                0 => "Оружие",
                1 => "Броня",
                2 => "Аксессуар",
                3 => "Экипировка",
                8 => "Свиток зачарования",
                15 => "Камень жизни",
                16 => "Красители",
                17 => "Кристалл",
                18 => "Книга заклинаний",
                19 => "Усиление",
                20 => "Зелье/Свиток",
                21 => "Билет",
                22 => "Ремесло",
                24 => "Продукты",
                _ => $"Неизвестный ({subItemType})"
            };
        }

        /// <summary>
        /// Декодирование элементарных атрибутов
        /// </summary>
        public static string DecodeElementalAttribute(int attackType)
        {
            return attackType switch
            {
                -2 => "Нет атакующего атрибута",
                0 => "Огонь",
                1 => "Вода", 
                2 => "Ветер",
                3 => "Земля",
                4 => "Свет",
                5 => "Тьма",
                _ => $"Неизвестный ({attackType})"
            };
        }

        /// <summary>
        /// Декодирование типов ответов комиссии
        /// </summary>
        public static string DecodeCommissionReplyType(CommissionListReplyType replyType)
        {
            return replyType switch
            {
                CommissionListReplyType.PlayerAuctionsEmpty => "Аукционы игрока пусты",
                CommissionListReplyType.ItemDoesNotExist => "Предмет не существует",
                CommissionListReplyType.PlayerAuctions => "Аукционы игрока",
                CommissionListReplyType.Auctions => "Все аукционы",
                _ => $"Неизвестный ({replyType})"
            };
        }

        /// <summary>
        /// Декодирование подтипов World Exchange
        /// </summary>
        public static string DecodeWorldExchangeSubType(WorldExchangeItemSubType subType)
        {
            return subType switch
            {
                WorldExchangeItemSubType.WEAPON => "Оружие",
                WorldExchangeItemSubType.ARMOR => "Броня",
                WorldExchangeItemSubType.ACCESSORY => "Аксессуар",
                WorldExchangeItemSubType.MATERIAL => "Материал",
                WorldExchangeItemSubType.CONSUMABLE => "Расходник",
                WorldExchangeItemSubType.ETC => "Прочее",
                WorldExchangeItemSubType.PET => "Питомец",
                WorldExchangeItemSubType.SKILL => "Навык",
                WorldExchangeItemSubType.ENCHANT => "Зачарование",
                WorldExchangeItemSubType.SPECIAL => "Особый",
                _ => $"Неизвестный ({subType})"
            };
        }

        /// <summary>
        /// Форматирование цены
        /// </summary>
        public static string FormatPrice(long price)
        {
            if (price >= 1_000_000_000)
                return $"{price / 1_000_000_000.0:F1}B";
            if (price >= 1_000_000)
                return $"{price / 1_000_000.0:F1}M";
            if (price >= 1_000)
                return $"{price / 1_000.0:F1}K";
            return price.ToString("N0");
        }

        /// <summary>
        /// Форматирование времени окончания
        /// </summary>
        public static string FormatEndTime(int endTime)
        {
            var currentTime = (int)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var remaining = endTime - currentTime;
            
            if (remaining <= 0)
                return "Истек";
            
            var days = remaining / 86400;
            var hours = (remaining % 86400) / 3600;
            var minutes = (remaining % 3600) / 60;
            
            if (days > 0)
                return $"{days}д {hours}ч";
            if (hours > 0)
                return $"{hours}ч {minutes}м";
            return $"{minutes}м";
        }

        /// <summary>
        /// Форматирование уровня зачарования
        /// </summary>
        public static string FormatEnchantLevel(int enchantLevel)
        {
            if (enchantLevel == 0)
                return "";
            if (enchantLevel > 0)
                return $"+{enchantLevel}";
            return enchantLevel.ToString();
        }

        /// <summary>
        /// Получить цвет для уровня зачарования
        /// </summary>
        public static string GetEnchantColor(int enchantLevel)
        {
            return enchantLevel switch
            {
                >= 16 => "#FFD700", // Золотой
                >= 10 => "#FF6B6B", // Красный
                >= 5 => "#4ECDC4",  // Бирюзовый
                >= 1 => "#45B7D1",  // Синий
                _ => "#95A5A6"      // Серый
            };
        }

        /// <summary>
        /// Получить цвет для цены
        /// </summary>
        public static string GetPriceColor(long price, long averagePrice)
        {
            if (averagePrice == 0)
                return "#95A5A6"; // Серый
            
            var ratio = (double)price / averagePrice;
            return ratio switch
            {
                <= 0.5 => "#2ECC71", // Зеленый (дешево)
                <= 0.8 => "#F39C12", // Оранжевый (нормально)
                <= 1.2 => "#E67E22", // Темно-оранжевый (дорого)
                _ => "#E74C3C"       // Красный (очень дорого)
            };
        }

        /// <summary>
        /// Получить все элементарные атрибуты
        /// </summary>
        public static string[] GetElementNames()
        {
            return new[] { "Огонь", "Вода", "Ветер", "Земля", "Свет", "Тьма" };
        }

        /// <summary>
        /// Получить все типы магазинов
        /// </summary>
        public static Dictionary<StoreType, string> GetStoreTypes()
        {
            return new Dictionary<StoreType, string>
            {
                { StoreType.Sell, "Продажа" },
                { StoreType.Buy, "Покупка" },
                { StoreType.All, "Все" }
            };
        }

        /// <summary>
        /// Получить все типы предметов
        /// </summary>
        public static Dictionary<StoreItemType, string> GetItemTypes()
        {
            return new Dictionary<StoreItemType, string>
            {
                { StoreItemType.All, "Все" },
                { StoreItemType.Equipment, "Экипировка" },
                { StoreItemType.EnhancementOrExping, "Усиление/Опыт" },
                { StoreItemType.GroceryOrCollectionMisc, "Продукты/Коллекция" }
            };
        }

        /// <summary>
        /// Получить все подтипы предметов
        /// </summary>
        public static Dictionary<StoreSubItemType, string> GetSubItemTypes()
        {
            return new Dictionary<StoreSubItemType, string>
            {
                { StoreSubItemType.All, "Все" },
                { StoreSubItemType.Weapon, "Оружие" },
                { StoreSubItemType.Armor, "Броня" },
                { StoreSubItemType.Accessory, "Аксессуар" },
                { StoreSubItemType.EquipmentMisc, "Экипировка" },
                { StoreSubItemType.EnchantScroll, "Свиток зачарования" },
                { StoreSubItemType.LifeStone, "Камень жизни" },
                { StoreSubItemType.Dyes, "Красители" },
                { StoreSubItemType.Crystal, "Кристалл" },
                { StoreSubItemType.Spellbook, "Книга заклинаний" },
                { StoreSubItemType.EnhancementMisc, "Усиление" },
                { StoreSubItemType.PotionScroll, "Зелье/Свиток" },
                { StoreSubItemType.Ticket, "Билет" },
                { StoreSubItemType.PackCraft, "Ремесло" },
                { StoreSubItemType.GroceryMisc, "Продукты" }
            };
        }
    }
}
