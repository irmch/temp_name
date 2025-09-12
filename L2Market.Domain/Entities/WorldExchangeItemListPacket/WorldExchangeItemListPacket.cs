using System;
using System.Collections.Generic;
using System.IO;

namespace L2Market.Domain.Entities.WorldExchangeItemListPacket
{
    /// <summary>
    /// Пакет списка предметов World Exchange
    /// </summary>
    public class WorldExchangeItemListPacket
    {
        private int _category;
        private int _sortType;
        private int _page;
        private int _itemsCount;
        private readonly List<WorldExchangeItemInfo> _items;

        public int Category => _category;
        public int SortType => _sortType;
        public int Page => _page;
        public int ItemsCount => _itemsCount;
        public IReadOnlyList<WorldExchangeItemInfo> Items => _items.AsReadOnly();

        public WorldExchangeItemListPacket()
        {
            _items = new List<WorldExchangeItemInfo>();
        }

        /// <summary>
        /// Парсит пакет World Exchange Item List из байтов
        /// </summary>
        public static WorldExchangeItemListPacket FromBytes(byte[] data)
        {
            using var stream = new MemoryStream(data);
            using var reader = new BinaryReader(stream);

            var packet = new WorldExchangeItemListPacket();

            // Читаем основные поля согласно Java коду
            packet._category = reader.ReadUInt16();  // buffer.writeShort(_type.getId())
            packet._sortType = reader.ReadByte();    // buffer.writeByte(0)
            packet._page = reader.ReadInt32();       // buffer.writeInt(0)
            packet._itemsCount = reader.ReadInt32(); // buffer.writeInt(_holders.size())

            // Обработка пустого списка
            if (packet._itemsCount == 0)
            {
                return packet;
            }

            // Читаем предметы согласно Java коду getItemInfo()
            for (int i = 0; i < packet._itemsCount; i++)
            {
                try
                {
                    var item = ReadWorldExchangeItem(reader);
                    packet._items.Add(item);
                }
                catch (Exception ex)
                {
                    throw new InvalidDataException($"Ошибка при чтении предмета {i + 1}: {ex.Message}");
                }
            }

            return packet;
        }

        private static WorldExchangeItemInfo ReadWorldExchangeItem(BinaryReader reader)
        {
            // Читаем поля согласно Java коду getItemInfo() и Python коду
            var worldExchangeId = reader.ReadUInt64();        // buffer.writeLong(holder.getWorldExchangeId())
            var price = reader.ReadUInt64();                  // buffer.writeLong(holder.getPrice())
            var endTime = reader.ReadInt32();                 // buffer.writeInt((int) (holder.getEndTime() / 1000L))
            var itemId = reader.ReadInt32();                  // buffer.writeInt(item.getId())
            var count = reader.ReadUInt64();                  // buffer.writeLong(item.getCount())
            var enchantLevel = reader.ReadInt32();            // buffer.writeInt(item.getEnchantLevel() < 1 ? 0 : item.getEnchantLevel())
            var augmentationOption1 = reader.ReadInt32();     // buffer.writeInt(iv != null ? iv.getOption1Id() : 0)
            var augmentationOption2 = reader.ReadInt32();     // buffer.writeInt(iv != null ? iv.getOption2Id() : 0)
            var unk = reader.ReadInt32();                     // unk field
            var unknownField = reader.ReadInt32();            // buffer.writeInt(-1)
            var attackAttributeType = reader.ReadUInt16();    // buffer.writeShort(item.getAttackAttribute() != null ? item.getAttackAttribute().getType().getClientId() : 0)
            var attackAttributeValue = reader.ReadUInt16();   // buffer.writeShort(item.getAttackAttribute() != null ? item.getAttackAttribute().getValue() : 0)
            var defenceFire = reader.ReadUInt16();            // buffer.writeShort(item.getDefenceAttribute(AttributeType.FIRE))
            var defenceWater = reader.ReadUInt16();           // buffer.writeShort(item.getDefenceAttribute(AttributeType.WATER))
            var defenceWind = reader.ReadUInt16();            // buffer.writeShort(item.getDefenceAttribute(AttributeType.WIND))
            var defenceEarth = reader.ReadUInt16();           // buffer.writeShort(item.getDefenceAttribute(AttributeType.EARTH))
            var defenceHoly = reader.ReadUInt16();            // buffer.writeShort(item.getDefenceAttribute(AttributeType.HOLY))
            var defenceDark = reader.ReadUInt16();            // buffer.writeShort(item.getDefenceAttribute(AttributeType.DARK))
            var visualId = reader.ReadInt32();                // buffer.writeInt(item.getVisualId())
            var soulCrystalOption1 = reader.ReadInt32();      // Soul Crystal Options - 3 int'а
            var soulCrystalOption2 = reader.ReadInt32();
            var soulCrystalSpecialOption = reader.ReadInt32();
            var isBlessed = reader.ReadUInt16();              // buffer.writeShort(0); // isBlessed

            return new WorldExchangeItemInfo(
                worldExchangeId,
                price,
                endTime,
                itemId,
                count,
                enchantLevel,
                augmentationOption1,
                augmentationOption2,
                unknownField,
                attackAttributeType,
                attackAttributeValue,
                defenceFire,
                defenceWater,
                defenceWind,
                defenceEarth,
                defenceHoly,
                defenceDark,
                visualId,
                soulCrystalOption1,
                soulCrystalOption2,
                soulCrystalSpecialOption,
                isBlessed
            );
        }

        public override string ToString()
        {
            return $"WorldExchangeItemListPacket(category={_category}, sortType={_sortType}, page={_page}, itemsCount={_items.Count})";
        }
    }
}
