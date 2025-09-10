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
        // private int _packetId; // Не используется в текущей реализации
        // private int _extendedId; // Не используется в текущей реализации
        private int _category;
        private int _sortType;
        private int _page;
        private int _itemsCount;
        private readonly List<WorldExchangeItemInfo> _items;

        // public int PacketId => _packetId; // Не используется в текущей реализации
        // public int ExtendedId => _extendedId; // Не используется в текущей реализации
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

            packet._category = reader.ReadUInt16();
            packet._sortType = reader.ReadByte();
            packet._page = reader.ReadInt32();
            packet._itemsCount = reader.ReadInt32();

            // Читаем предметы
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
            // Ищем начало предмета (пропускаем нули и маркеры FE FF)
            var startPos = reader.BaseStream.Position;
            var data = reader.BaseStream as MemoryStream;

            // Ищем следующий маркер FE FF
            long feFfPos = -1;
            if (data != null)
            {
                var buffer = data.ToArray();
                for (long i = startPos; i < buffer.Length - 1; i++)
                {
                    if (buffer[i] == 0xFE && buffer[i + 1] == 0xFF)
                    {
                        feFfPos = i;
                        break;
                    }
                }
            }

            // Читаем данные предмета
            var worldExchangeId = reader.ReadUInt64();
            var price = reader.ReadUInt64();
            var endTime = reader.ReadInt32();
            var itemId = reader.ReadInt32();
            var count = reader.ReadUInt64();
            var enchantLevel = reader.ReadInt32();

            // Читаем augmentation options
            var augmentationOption1 = reader.ReadInt32();
            var augmentationOption2 = reader.ReadInt32();
            var unknownField = reader.ReadInt32();

            // Читаем атакующие атрибуты
            var attackAttributeType = reader.ReadUInt16();
            var attackAttributeValue = reader.ReadUInt16();

            // Читаем защитные атрибуты
            var defenceFire = reader.ReadUInt16();
            var defenceWater = reader.ReadUInt16();
            var defenceWind = reader.ReadUInt16();
            var defenceEarth = reader.ReadUInt16();
            var defenceHoly = reader.ReadUInt16();
            var defenceDark = reader.ReadUInt16();

            // Читаем visual ID
            var visualId = reader.ReadInt32();

            // Читаем soul crystal options
            var soulCrystalOption1 = reader.ReadInt32();
            var soulCrystalOption2 = reader.ReadInt32();
            var soulCrystalSpecialOption = reader.ReadInt32();

            // Читаем blessed статус
            var isBlessed = reader.ReadUInt16();

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
