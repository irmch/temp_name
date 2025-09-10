using System;

namespace L2Market.Domain.Entities.WorldExchangeItemListPacket
{
    /// <summary>
    /// Информация о предмете в World Exchange
    /// </summary>
    public class WorldExchangeItemInfo
    {
        private readonly ulong _worldExchangeId;
        private readonly ulong _price;
        private readonly int _endTime;
        private readonly int _itemId;
        private readonly ulong _count;
        private readonly int _enchantLevel;
        private readonly int _augmentationOption1;
        private readonly int _augmentationOption2;
        private readonly int _unknownField;
        private readonly ushort _attackAttributeType;
        private readonly ushort _attackAttributeValue;
        private readonly ushort _defenceFire;
        private readonly ushort _defenceWater;
        private readonly ushort _defenceWind;
        private readonly ushort _defenceEarth;
        private readonly ushort _defenceHoly;
        private readonly ushort _defenceDark;
        private readonly int _visualId;
        private readonly int _soulCrystalOption1;
        private readonly int _soulCrystalOption2;
        private readonly int _soulCrystalSpecialOption;
        private readonly ushort _isBlessed;

        public ulong WorldExchangeId => _worldExchangeId;
        public ulong Price => _price;
        public int EndTime => _endTime;
        public int ItemId => _itemId;
        public ulong Count => _count;
        public int EnchantLevel => _enchantLevel;
        public int AugmentationOption1 => _augmentationOption1;
        public int AugmentationOption2 => _augmentationOption2;
        public int UnknownField => _unknownField;
        public ushort AttackAttributeType => _attackAttributeType;
        public ushort AttackAttributeValue => _attackAttributeValue;
        public ushort DefenceFire => _defenceFire;
        public ushort DefenceWater => _defenceWater;
        public ushort DefenceWind => _defenceWind;
        public ushort DefenceEarth => _defenceEarth;
        public ushort DefenceHoly => _defenceHoly;
        public ushort DefenceDark => _defenceDark;
        public int VisualId => _visualId;
        public int SoulCrystalOption1 => _soulCrystalOption1;
        public int SoulCrystalOption2 => _soulCrystalOption2;
        public int SoulCrystalSpecialOption => _soulCrystalSpecialOption;
        public ushort IsBlessed => _isBlessed;

        public WorldExchangeItemInfo(
            ulong worldExchangeId,
            ulong price,
            int endTime,
            int itemId,
            ulong count,
            int enchantLevel,
            int augmentationOption1,
            int augmentationOption2,
            int unknownField,
            ushort attackAttributeType,
            ushort attackAttributeValue,
            ushort defenceFire,
            ushort defenceWater,
            ushort defenceWind,
            ushort defenceEarth,
            ushort defenceHoly,
            ushort defenceDark,
            int visualId,
            int soulCrystalOption1,
            int soulCrystalOption2,
            int soulCrystalSpecialOption,
            ushort isBlessed)
        {
            _worldExchangeId = worldExchangeId;
            _price = price;
            _endTime = endTime;
            _itemId = itemId;
            _count = count;
            _enchantLevel = enchantLevel;
            _augmentationOption1 = augmentationOption1;
            _augmentationOption2 = augmentationOption2;
            _unknownField = unknownField;
            _attackAttributeType = attackAttributeType;
            _attackAttributeValue = attackAttributeValue;
            _defenceFire = defenceFire;
            _defenceWater = defenceWater;
            _defenceWind = defenceWind;
            _defenceEarth = defenceEarth;
            _defenceHoly = defenceHoly;
            _defenceDark = defenceDark;
            _visualId = visualId;
            _soulCrystalOption1 = soulCrystalOption1;
            _soulCrystalOption2 = soulCrystalOption2;
            _soulCrystalSpecialOption = soulCrystalSpecialOption;
            _isBlessed = isBlessed;
        }

        public override string ToString()
        {
            return $"WorldExchangeItemInfo(id={_itemId}, count={_count}, enchant={_enchantLevel}, price={_price:N0})";
        }
    }
}
