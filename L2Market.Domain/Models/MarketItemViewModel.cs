using System;
using System.ComponentModel;
using L2Market.Domain.Entities.ExPrivateStoreSearchItemPacket;
using L2Market.Domain.Entities.ExResponseCommissionListPacket;
using L2Market.Domain.Entities.WorldExchangeItemListPacket;

namespace L2Market.Domain.Models
{
    /// <summary>
    /// Модель для отображения предмета в UI
    /// </summary>
    public class MarketItemViewModel : INotifyPropertyChanged
    {
        private string _itemName = string.Empty;
        private long _price;
        private string _sellerName = string.Empty;
        private string _marketType = string.Empty;
        private int _enchantLevel;
        private bool _hasAugmentation;
        private bool _hasSoulCrystal;
        private bool _isBlessed;
        private DateTime _lastSeen;
        private string _itemId = string.Empty;
        private string _additionalInfo = string.Empty;
        
        // Дополнительные поля
        private long _count;
        private int _itemType2;
        private long _bodyPart;
        private int _mana;
        private int _time;
        private bool _available;
        private int _visualId;
        private string _coordinates = string.Empty;
        private string _endTime = string.Empty;
        private string _attackAttribute = string.Empty;
        private string _defenceAttributes = string.Empty;
        private string _soulCrystalInfo = string.Empty;
        
        // Недостающие поля
        private int _objectId;
        private int _location;
        private int _customType1;
        private int _equipped;
        private string _augmentationInfo = string.Empty;
        private string _elementalAttrs = string.Empty;
        private string _enchantEffects = string.Empty;
        private int _reuseDelay;
        private string _commissionId = string.Empty;
        private string _commissionType = string.Empty;
        private string _durationType = string.Empty;
        private string _worldExchangeId = string.Empty;
        private int _unknownField;
        private int _vendorObjectId;
 
        public string ItemName
        {
            get => _itemName;
            set { _itemName = value; OnPropertyChanged(nameof(ItemName)); }
        }

        public long Price
        {
            get => _price;
            set { _price = value; OnPropertyChanged(nameof(Price)); }
        }

        public string SellerName
        {
            get => _sellerName;
            set { _sellerName = value; OnPropertyChanged(nameof(SellerName)); }
        }

        public string MarketType
        {
            get => _marketType;
            set { _marketType = value; OnPropertyChanged(nameof(MarketType)); }
        }

        public int EnchantLevel
        {
            get => _enchantLevel;
            set { _enchantLevel = value; OnPropertyChanged(nameof(EnchantLevel)); }
        }

        public bool HasAugmentation
        {
            get => _hasAugmentation;
            set { _hasAugmentation = value; OnPropertyChanged(nameof(HasAugmentation)); }
        }

        public bool HasSoulCrystal
        {
            get => _hasSoulCrystal;
            set { _hasSoulCrystal = value; OnPropertyChanged(nameof(HasSoulCrystal)); }
        }

        public bool IsBlessed
        {
            get => _isBlessed;
            set { _isBlessed = value; OnPropertyChanged(nameof(IsBlessed)); }
        }

        public DateTime LastSeen
        {
            get => _lastSeen;
            set { _lastSeen = value; OnPropertyChanged(nameof(LastSeen)); }
        }

        public string ItemId
        {
            get => _itemId;
            set { _itemId = value; OnPropertyChanged(nameof(ItemId)); }
        }

        public string AdditionalInfo
        {
            get => _additionalInfo;
            set { _additionalInfo = value; OnPropertyChanged(nameof(AdditionalInfo)); }
        }

        public long Count
        {
            get => _count;
            set { _count = value; OnPropertyChanged(nameof(Count)); }
        }

        public int ItemType2
        {
            get => _itemType2;
            set { _itemType2 = value; OnPropertyChanged(nameof(ItemType2)); }
        }

        public long BodyPart
        {
            get => _bodyPart;
            set { _bodyPart = value; OnPropertyChanged(nameof(BodyPart)); }
        }

        public int Mana
        {
            get => _mana;
            set { _mana = value; OnPropertyChanged(nameof(Mana)); }
        }

        public int Time
        {
            get => _time;
            set { _time = value; OnPropertyChanged(nameof(Time)); }
        }

        public bool Available
        {
            get => _available;
            set { _available = value; OnPropertyChanged(nameof(Available)); }
        }

        public int VisualId
        {
            get => _visualId;
            set { _visualId = value; OnPropertyChanged(nameof(VisualId)); }
        }

        public string Coordinates
        {
            get => _coordinates;
            set { _coordinates = value; OnPropertyChanged(nameof(Coordinates)); }
        }

        public string EndTime
        {
            get => _endTime;
            set { _endTime = value; OnPropertyChanged(nameof(EndTime)); }
        }

        public string AttackAttribute
        {
            get => _attackAttribute;
            set { _attackAttribute = value; OnPropertyChanged(nameof(AttackAttribute)); }
        }

        public string DefenceAttributes
        {
            get => _defenceAttributes;
            set { _defenceAttributes = value; OnPropertyChanged(nameof(DefenceAttributes)); }
        }

        public string SoulCrystalInfo
        {
            get => _soulCrystalInfo;
            set { _soulCrystalInfo = value; OnPropertyChanged(nameof(SoulCrystalInfo)); }
        }

        public int ObjectId
        {
            get => _objectId;
            set { _objectId = value; OnPropertyChanged(nameof(ObjectId)); }
        }

        public int Location
        {
            get => _location;
            set { _location = value; OnPropertyChanged(nameof(Location)); }
        }

        public int CustomType1
        {
            get => _customType1;
            set { _customType1 = value; OnPropertyChanged(nameof(CustomType1)); }
        }

        public int Equipped
        {
            get => _equipped;
            set { _equipped = value; OnPropertyChanged(nameof(Equipped)); }
        }

        public string AugmentationInfo
        {
            get => _augmentationInfo;
            set { _augmentationInfo = value; OnPropertyChanged(nameof(AugmentationInfo)); }
        }

        public string ElementalAttrs
        {
            get => _elementalAttrs;
            set { _elementalAttrs = value; OnPropertyChanged(nameof(ElementalAttrs)); }
        }

        public string EnchantEffects
        {
            get => _enchantEffects;
            set { _enchantEffects = value; OnPropertyChanged(nameof(EnchantEffects)); }
        }

        public int ReuseDelay
        {
            get => _reuseDelay;
            set { _reuseDelay = value; OnPropertyChanged(nameof(ReuseDelay)); }
        }

        public string CommissionId
        {
            get => _commissionId;
            set { _commissionId = value; OnPropertyChanged(nameof(CommissionId)); }
        }

        public string CommissionType
        {
            get => _commissionType;
            set { _commissionType = value; OnPropertyChanged(nameof(CommissionType)); }
        }

        public string DurationType
        {
            get => _durationType;
            set { _durationType = value; OnPropertyChanged(nameof(DurationType)); }
        }

        public string WorldExchangeId
        {
            get => _worldExchangeId;
            set { _worldExchangeId = value; OnPropertyChanged(nameof(WorldExchangeId)); }
        }

        public int UnknownField
        {
            get => _unknownField;
            set { _unknownField = value; OnPropertyChanged(nameof(UnknownField)); }
        }

        public int VendorObjectId
        {
            get => _vendorObjectId;
            set { _vendorObjectId = value; OnPropertyChanged(nameof(VendorObjectId)); }
        }

        // Форматированные свойства для отображения
        public string FormattedPrice => FormatPrice(_price);
        public string FormattedEnchantLevel => _enchantLevel > 0 ? $"+{_enchantLevel}" : "";
        public string FormattedLastSeen => _lastSeen.ToString("HH:mm:ss");
        public string StatusIcons => GetStatusIcons();
        
        // Индикация времени до исчезновения
        public string TimeUntilExpiry
        {
            get
            {
                var timeSinceLastSeen = DateTime.UtcNow - _lastSeen;
                var timeLeft = GetItemLifetime() - timeSinceLastSeen;
                
                if (timeLeft <= TimeSpan.Zero)
                    return "Истек";
                
                if (timeLeft.TotalMinutes < 1)
                    return $"<1 мин";
                
                return $"{(int)timeLeft.TotalMinutes} мин";
            }
        }
        
        // Цвет индикации времени
        public string TimeUntilExpiryColor
        {
            get
            {
                var timeSinceLastSeen = DateTime.UtcNow - _lastSeen;
                var timeLeft = GetItemLifetime() - timeSinceLastSeen;
                
                if (timeLeft <= TimeSpan.Zero)
                    return "#E74C3C"; // Красный - истек
                
                if (timeLeft.TotalMinutes < 1)
                    return "#F39C12"; // Оранжевый - менее минуты
                
                if (timeLeft.TotalMinutes < 2)
                    return "#F1C40F"; // Желтый - менее 2 минут
                
                return "#27AE60"; // Зеленый - нормально
            }
        }
        
        // Условное форматирование для "Все" магазинов
        public string FormattedCoordinates => string.IsNullOrEmpty(_coordinates) ? "—" : _coordinates;
        public string FormattedEndTime => string.IsNullOrEmpty(_endTime) ? "—" : _endTime;
        public string FormattedAttackAttribute => string.IsNullOrEmpty(_attackAttribute) ? "—" : _attackAttribute;
        public string FormattedDefenceAttributes => string.IsNullOrEmpty(_defenceAttributes) ? "—" : _defenceAttributes;
        public string FormattedSoulCrystalInfo => string.IsNullOrEmpty(_soulCrystalInfo) ? "—" : _soulCrystalInfo;
        
        // Цветовое кодирование для разных типов магазинов
        public string MarketTypeColor
        {
            get
            {
                if (_marketType.Contains("Частный магазин"))
                {
                    if (_marketType.Contains("Продажа")) return "#27AE60";      // Зеленый
                    if (_marketType.Contains("Покупка")) return "#E74C3C";      // Красный
                    if (_marketType.Contains("Пакетная продажа")) return "#F39C12"; // Оранжевый
                    return "#3498DB"; // Синий по умолчанию
                }
                
                return _marketType switch
                {
                    "Комиссия" => "#E67E22",        // Оранжевый
                    "Мировой обмен" => "#9B59B6",   // Фиолетовый
                    _ => "#95A5A6"                  // Серый
                };
            }
        }
        
        // Иконка для типа магазина
        public string MarketTypeIcon
        {
            get
            {
                if (_marketType.Contains("Частный магазин"))
                {
                    if (_marketType.Contains("Продажа")) return "💰";      // Продажа
                    if (_marketType.Contains("Покупка")) return "🛒";      // Покупка
                    if (_marketType.Contains("Пакетная продажа")) return "📦"; // Пакетная продажа
                    return "🏪"; // Магазин по умолчанию
                }
                
                return _marketType switch
                {
                    "Комиссия" => "🏛️",
                    "Мировой обмен" => "🌍",
                    _ => "❓"
                };
            }
        }

        private string FormatPrice(long price)
        {
            if (price >= 1_000_000_000)
                return $"{price / 1_000_000_000.0:F1}B";
            if (price >= 1_000_000)
                return $"{price / 1_000_000.0:F1}M";
            if (price >= 1_000)
                return $"{price / 1_000.0:F1}K";
            return price.ToString("N0");
        }

        private string GetStatusIcons()
        {
            var icons = "";
            if (_hasAugmentation) icons += "⚡";
            if (_hasSoulCrystal) icons += "💎";
            if (_isBlessed) icons += "✨";
            return icons;
        }

        private TimeSpan GetItemLifetime()
        {
            return _marketType switch
            {
                var type when type.Contains("Частный магазин") => TimeSpan.FromMinutes(5),
                var type when type.Contains("Комиссия") => TimeSpan.FromMinutes(10),
                var type when type.Contains("Мировой обмен") => TimeSpan.FromMinutes(15),
                _ => TimeSpan.FromMinutes(5)
            };
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // Фабричные методы для создания из разных типов предметов
        public static MarketItemViewModel FromPrivateStoreItem(PrivateStoreItem item)
        {
            var storeTypeText = L2Market.Domain.Entities.ExPrivateStoreSearchItemPacket.StoreDecoders.DecodeStoreType(item.StoreType);
            var itemTypeText = L2Market.Domain.Entities.ExPrivateStoreSearchItemPacket.StoreDecoders.DecodeItemType2(item.ItemInfo.ItemType2);
            var bodyPartText = L2Market.Domain.Entities.ExPrivateStoreSearchItemPacket.StoreDecoders.DecodeBodyPart(item.ItemInfo.BodyPart);
            
            return new MarketItemViewModel
            {
                ItemName = $"Item {item.ItemInfo.ItemId}",
                Price = item.Price,
                SellerName = item.VendorName,
                MarketType = $"Частный магазин ({storeTypeText})",
                EnchantLevel = item.ItemInfo.EnchantLevel,
                HasAugmentation = item.ItemInfo.Augmentation?.Any() == true,
                HasSoulCrystal = item.ItemInfo.SoulCrystalOptions?.Any() == true,
                IsBlessed = item.ItemInfo.Blessed == true,
                LastSeen = DateTime.UtcNow,
                ItemId = item.ItemInfo.ItemId.ToString(),
                AdditionalInfo = $"{itemTypeText} | {bodyPartText}",
                Count = item.ItemInfo.Count,
                ItemType2 = item.ItemInfo.ItemType2,
                BodyPart = item.ItemInfo.BodyPart,
                Mana = item.ItemInfo.Mana,
                Time = item.ItemInfo.Time,
                Available = item.ItemInfo.Available,
                VisualId = item.ItemInfo.VisualId ?? 0,
                Coordinates = $"X:{item.VendorX} Y:{item.VendorY} Z:{item.VendorZ}",
                SoulCrystalInfo = FormatSoulCrystalInfo(item.ItemInfo.SoulCrystalOptions, item.ItemInfo.SoulCrystalSpecialOptions),
                // Новые поля
                ObjectId = item.ItemInfo.ObjectId,
                Location = item.ItemInfo.Location,
                CustomType1 = item.ItemInfo.CustomType1,
                Equipped = item.ItemInfo.Equipped,
                AugmentationInfo = FormatAugmentationInfo(item.ItemInfo.Augmentation),
                ElementalAttrs = FormatElementalAttrs(item.ItemInfo.ElementalAttrs),
                EnchantEffects = FormatEnchantEffects(item.ItemInfo.EnchantEffects),
                ReuseDelay = item.ItemInfo.ReuseDelay ?? 0,
                VendorObjectId = item.VendorObjectId
            };
        }

        public static MarketItemViewModel FromCommissionItem(CommissionItem item)
        {
            return new MarketItemViewModel
            {
                ItemName = $"Item {item.ItemInfo.ItemId}",
                Price = item.PricePerUnit,
                SellerName = item.SellerName ?? "Неизвестно",
                MarketType = "Комиссия",
                EnchantLevel = item.ItemInfo.EnchantLevel,
                HasAugmentation = item.ItemInfo.Augmentation?.Any() == true,
                HasSoulCrystal = item.ItemInfo.SoulCrystalOptions?.Any() == true,
                IsBlessed = item.ItemInfo.Blessed == true,
                LastSeen = DateTime.UtcNow,
                ItemId = item.ItemInfo.ItemId.ToString(),
                AdditionalInfo = $"ID комиссии: {item.CommissionId}",
                Count = item.ItemInfo.Count,
                ItemType2 = item.ItemInfo.ItemType2,
                BodyPart = item.ItemInfo.BodyPart,
                Mana = item.ItemInfo.Mana,
                Time = item.ItemInfo.Time,
                Available = item.ItemInfo.Available,
                VisualId = item.ItemInfo.VisualId ?? 0,
                EndTime = FormatEndTime(item.EndTime),
                SoulCrystalInfo = FormatSoulCrystalInfo(item.ItemInfo.SoulCrystalOptions, item.ItemInfo.SoulCrystalSpecialOptions),
                // Новые поля
                ObjectId = item.ItemInfo.ObjectId,
                Location = item.ItemInfo.Location,
                CustomType1 = item.ItemInfo.CustomType1,
                Equipped = item.ItemInfo.Equipped,
                AugmentationInfo = FormatAugmentationInfo(item.ItemInfo.Augmentation),
                ElementalAttrs = FormatElementalAttrs(item.ItemInfo.ElementalAttrs),
                EnchantEffects = FormatEnchantEffects(item.ItemInfo.EnchantEffects),
                ReuseDelay = item.ItemInfo.ReuseDelay ?? 0,
                CommissionId = item.CommissionId.ToString(),
                CommissionType = DecodeCommissionType(item.CommissionItemType),
                DurationType = DecodeDurationType(item.DurationType)
            };
        }

        public static MarketItemViewModel FromWorldExchangeItem(WorldExchangeItemInfo item)
        {
            return new MarketItemViewModel
            {
                ItemName = $"Item {item.ItemId}",
                Price = (long)item.Price,
                SellerName = "Мировой обмен",
                MarketType = "Мировой обмен",
                EnchantLevel = item.EnchantLevel,
                HasAugmentation = item.AugmentationOption1 != 0 || item.AugmentationOption2 != 0,
                HasSoulCrystal = item.SoulCrystalOption1 != 0 || item.SoulCrystalOption2 != 0,
                IsBlessed = item.IsBlessed != 0,
                LastSeen = DateTime.UtcNow,
                ItemId = item.ItemId.ToString(),
                AdditionalInfo = $"ID обмена: {item.WorldExchangeId}",
                Count = (long)item.Count,
                VisualId = item.VisualId,
                EndTime = FormatEndTime(item.EndTime),
                AttackAttribute = FormatAttackAttribute(item.AttackAttributeType, item.AttackAttributeValue),
                DefenceAttributes = FormatDefenceAttributes(item.DefenceFire, item.DefenceWater, item.DefenceWind, 
                    item.DefenceEarth, item.DefenceHoly, item.DefenceDark),
                SoulCrystalInfo = FormatWorldExchangeSoulCrystal(item.SoulCrystalOption1, item.SoulCrystalOption2, item.SoulCrystalSpecialOption),
                // Новые поля
                WorldExchangeId = item.WorldExchangeId.ToString(),
                UnknownField = item.UnknownField
            };
        }

        // Вспомогательные методы форматирования
        private static string FormatEndTime(int endTime)
        {
            if (endTime == 0) return "Бессрочно";
            var dateTime = DateTimeOffset.FromUnixTimeSeconds(endTime).DateTime;
            return dateTime.ToString("dd.MM.yyyy HH:mm");
        }

        private static string FormatAttackAttribute(ushort type, ushort value)
        {
            if (type == 0 || value == 0) return "";
            return $"Атака {type}: {value}";
        }

        private static string FormatDefenceAttributes(ushort fire, ushort water, ushort wind, ushort earth, ushort holy, ushort dark)
        {
            var defences = new List<string>();
            if (fire > 0) defences.Add($"Огонь:{fire}");
            if (water > 0) defences.Add($"Вода:{water}");
            if (wind > 0) defences.Add($"Ветер:{wind}");
            if (earth > 0) defences.Add($"Земля:{earth}");
            if (holy > 0) defences.Add($"Свет:{holy}");
            if (dark > 0) defences.Add($"Тьма:{dark}");
            return string.Join(" ", defences);
        }

        private static string FormatSoulCrystalInfo(List<int>? options, List<int>? specialOptions)
        {
            var info = new List<string>();
            if (options?.Any() == true) info.Add($"Обычные: {string.Join(",", options)}");
            if (specialOptions?.Any() == true) info.Add($"Спец: {string.Join(",", specialOptions)}");
            return string.Join(" | ", info);
        }

        private static string FormatWorldExchangeSoulCrystal(int option1, int option2, int specialOption)
        {
            var info = new List<string>();
            if (option1 != 0) info.Add($"1:{option1}");
            if (option2 != 0) info.Add($"2:{option2}");
            if (specialOption != 0) info.Add($"Спец:{specialOption}");
            return string.Join(" ", info);
        }

        private static string FormatAugmentationInfo(Dictionary<string, int>? augmentation)
        {
            if (augmentation?.Any() != true) return "";
            return string.Join(", ", augmentation.Select(kvp => $"{kvp.Key}:{kvp.Value}"));
        }

        private static string FormatElementalAttrs(Dictionary<string, int>? elementalAttrs)
        {
            if (elementalAttrs?.Any() != true) return "";
            return string.Join(", ", elementalAttrs.Select(kvp => $"{kvp.Key}:{kvp.Value}"));
        }

        private static string FormatEnchantEffects(List<int>? enchantEffects)
        {
            if (enchantEffects?.Any() != true) return "";
            return string.Join(", ", enchantEffects);
        }

        private static string DecodeCommissionType(int commissionItemType)
        {
            return commissionItemType switch
            {
                0 => "Обычная",
                1 => "Премиум",
                2 => "VIP",
                _ => $"Неизвестный ({commissionItemType})"
            };
        }

        private static string DecodeDurationType(int durationType)
        {
            return durationType switch
            {
                0 => "1 день",
                1 => "3 дня",
                2 => "7 дней",
                3 => "14 дней",
                4 => "30 дней",
                _ => $"Неизвестный ({durationType})"
            };
        }
    }
}
