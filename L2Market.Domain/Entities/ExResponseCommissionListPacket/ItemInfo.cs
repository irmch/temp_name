using System.Collections.Generic;

namespace L2Market.Domain.Entities.ExResponseCommissionListPacket
{
    /// <summary>
    /// Информация о предмете в комиссии
    /// </summary>
    public class ItemInfo
    {
        private readonly int _mask;
        private readonly int _objectId;
        private readonly int _itemId;
        private readonly int _location;
        private readonly long _count;
        private readonly int _itemType2;
        private readonly int _customType1;
        private readonly int _equipped;
        private readonly long _bodyPart;
        private readonly int _enchantLevel;
        private readonly int _mana;
        private readonly int _time;
        private readonly bool _available;

        // Опциональные поля на основе маски
        private readonly Dictionary<string, int>? _augmentation;
        private readonly Dictionary<string, int>? _elementalAttrs;
        private readonly int? _visualId;
        private readonly List<int>? _soulCrystalOptions;
        private readonly List<int>? _soulCrystalSpecialOptions;
        private readonly List<int>? _enchantEffects;
        private readonly int? _reuseDelay;
        private readonly bool? _blessed;

        public int Mask => _mask;
        public int ObjectId => _objectId;
        public int ItemId => _itemId;
        public int Location => _location;
        public long Count => _count;
        public int ItemType2 => _itemType2;
        public int CustomType1 => _customType1;
        public int Equipped => _equipped;
        public long BodyPart => _bodyPart;
        public int EnchantLevel => _enchantLevel;
        public int Mana => _mana;
        public int Time => _time;
        public bool Available => _available;

        // Опциональные свойства
        public Dictionary<string, int>? Augmentation => _augmentation;
        public Dictionary<string, int>? ElementalAttrs => _elementalAttrs;
        public int? VisualId => _visualId;
        public List<int>? SoulCrystalOptions => _soulCrystalOptions;
        public List<int>? SoulCrystalSpecialOptions => _soulCrystalSpecialOptions;
        public List<int>? EnchantEffects => _enchantEffects;
        public int? ReuseDelay => _reuseDelay;
        public bool? Blessed => _blessed;

        public ItemInfo(
            int mask,
            int objectId,
            int itemId,
            int location,
            long count,
            int itemType2,
            int customType1,
            int equipped,
            long bodyPart,
            int enchantLevel,
            int mana,
            int time,
            bool available,
            Dictionary<string, int>? augmentation = null,
            Dictionary<string, int>? elementalAttrs = null,
            int? visualId = null,
            List<int>? soulCrystalOptions = null,
            List<int>? soulCrystalSpecialOptions = null,
            List<int>? enchantEffects = null,
            int? reuseDelay = null,
            bool? blessed = null)
        {
            _mask = mask;
            _objectId = objectId;
            _itemId = itemId;
            _location = location;
            _count = count;
            _itemType2 = itemType2;
            _customType1 = customType1;
            _equipped = equipped;
            _bodyPart = bodyPart;
            _enchantLevel = enchantLevel;
            _mana = mana;
            _time = time;
            _available = available;
            _augmentation = augmentation;
            _elementalAttrs = elementalAttrs;
            _visualId = visualId;
            _soulCrystalOptions = soulCrystalOptions;
            _soulCrystalSpecialOptions = soulCrystalSpecialOptions;
            _enchantEffects = enchantEffects;
            _reuseDelay = reuseDelay;
            _blessed = blessed;
        }

        public override string ToString()
        {
            return $"ItemInfo(id={_itemId}, count={_count}, enchant={_enchantLevel})";
        }
    }
}
