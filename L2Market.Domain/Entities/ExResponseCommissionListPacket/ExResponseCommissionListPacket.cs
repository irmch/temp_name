using System;
using System.Collections.Generic;
using System.IO;

namespace L2Market.Domain.Entities.ExResponseCommissionListPacket
{
    /// <summary>
    /// Пакет ответа на запрос списка комиссий
    /// </summary>
    public class ExResponseCommissionListPacket
    {
        private const int MaxChunkSize = 120;

        private CommissionListReplyType _replyType;
        private int _currentTime;
        private int _chunkId;
        private int _chunkSize;
        private readonly List<CommissionItem> _items;

        public CommissionListReplyType ReplyType => _replyType;
        public int CurrentTime => _currentTime;
        public int ChunkId => _chunkId;
        public int ChunkSize => _chunkSize;
        public IReadOnlyList<CommissionItem> Items => _items.AsReadOnly();

        public ExResponseCommissionListPacket()
        {
            _items = new List<CommissionItem>();
        }

        /// <summary>
        /// Парсит пакет ExResponseCommissionList из байтов
        /// </summary>
        public static ExResponseCommissionListPacket FromBytes(byte[] data)
        {
            using var stream = new MemoryStream(data);
            using var reader = new BinaryReader(stream);


            var packet = new ExResponseCommissionListPacket();

            // Читаем reply type (signed int)
            int replyTypeValue = reader.ReadInt32();
            try
            {
                packet._replyType = (CommissionListReplyType)replyTypeValue;
            }
            catch (ArgumentException)
            {
                packet._replyType = CommissionListReplyType.ItemDoesNotExist;
            }

            // Обрабатываем только если есть данные для чтения
            if (packet._replyType == CommissionListReplyType.PlayerAuctions || 
                packet._replyType == CommissionListReplyType.Auctions)
            {
                // Читаем текущее время (epoch seconds)
                packet._currentTime = reader.ReadInt32();
                
                // Читаем chunk ID
                packet._chunkId = reader.ReadInt32();
                
                // Читаем размер chunk'а
                packet._chunkSize = reader.ReadInt32();

                // Читаем предметы
                for (int i = 0; i < packet._chunkSize; i++)
                {
                    try
                    {
                        var item = ReadCommissionItem(reader);
                        packet._items.Add(item);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidDataException($"Ошибка при чтении предмета {i + 1}: {ex.Message}");
                    }
                }
            }

            return packet;
        }

        private static CommissionItem ReadCommissionItem(BinaryReader reader)
        {
            // Читаем поля CommissionItem
            long commissionId = reader.ReadInt64();
            long pricePerUnit = reader.ReadInt64();
            int commissionItemType = reader.ReadInt32();
            int durationType = reader.ReadInt32();
            int endTime = reader.ReadInt32();

            // Читаем имя продавца (null-terminated string)
            string? sellerName = ReadNullString(reader);

            // Читаем ItemInfo
            var itemInfo = ReadItemInfo(reader);

            return new CommissionItem(
                commissionId,
                pricePerUnit,
                commissionItemType,
                durationType,
                endTime,
                sellerName,
                itemInfo
            );
        }

        private static ItemInfo ReadItemInfo(BinaryReader reader)
        {
            // Читаем основные поля предмета
            ushort mask = reader.ReadUInt16();
            int objectId = reader.ReadInt32();
            int itemId = reader.ReadInt32();
            byte location = reader.ReadByte();
            long count = reader.ReadInt64();
            byte itemType2 = reader.ReadByte();
            byte customType1 = reader.ReadByte();
            ushort equipped = reader.ReadUInt16();
            long bodyPart = reader.ReadInt64();
            ushort enchantLevel = reader.ReadUInt16();
            int mana = reader.ReadInt32();
            byte protocol270 = reader.ReadByte();
            int timeValue = reader.ReadInt32();
            bool available = reader.ReadByte() != 0;
            ushort locked = reader.ReadUInt16();

            // Создаем базовый ItemInfo
            var itemInfo = new ItemInfo(
                mask,
                objectId,
                itemId,
                location,
                count,
                itemType2,
                customType1,
                equipped,
                bodyPart,
                enchantLevel,
                mana,
                timeValue,
                available
            );

            // Читаем опциональные поля на основе маски
            if ((mask & (int)ItemListType.AugmentBonus) != 0)
            {
                int option1 = reader.ReadInt32();
                int option2 = reader.ReadInt32();
                var augmentation = new Dictionary<string, int>
                {
                    ["option1"] = option1,
                    ["option2"] = option2
                };
                itemInfo = new ItemInfo(
                    mask, objectId, itemId, location, count, itemType2, customType1,
                    equipped, bodyPart, enchantLevel, mana, timeValue, available,
                    augmentation, itemInfo.ElementalAttrs, itemInfo.VisualId,
                    itemInfo.SoulCrystalOptions, itemInfo.SoulCrystalSpecialOptions,
                    itemInfo.EnchantEffects, itemInfo.ReuseDelay, itemInfo.Blessed
                );
            }

            if ((mask & (int)ItemListType.ElementalAttribute) != 0)
            {
                short attackType = reader.ReadInt16();
                short attackPower = reader.ReadInt16();
                
                // Пропускаем защиты (6 значений по 2 байта)
                for (int i = 0; i < 6; i++)
                {
                    reader.ReadInt16();
                }

                var elementalAttrs = new Dictionary<string, int>
                {
                    ["attack_type"] = attackType,
                    ["attack_power"] = attackPower
                };
                itemInfo = new ItemInfo(
                    mask, objectId, itemId, location, count, itemType2, customType1,
                    equipped, bodyPart, enchantLevel, mana, timeValue, available,
                    itemInfo.Augmentation, elementalAttrs, itemInfo.VisualId,
                    itemInfo.SoulCrystalOptions, itemInfo.SoulCrystalSpecialOptions,
                    itemInfo.EnchantEffects, itemInfo.ReuseDelay, itemInfo.Blessed
                );
            }

            if ((mask & (int)ItemListType.VisualId) != 0)
            {
                int visualId = reader.ReadInt32();
                itemInfo = new ItemInfo(
                    mask, objectId, itemId, location, count, itemType2, customType1,
                    equipped, bodyPart, enchantLevel, mana, timeValue, available,
                    itemInfo.Augmentation, itemInfo.ElementalAttrs, visualId,
                    itemInfo.SoulCrystalOptions, itemInfo.SoulCrystalSpecialOptions,
                    itemInfo.EnchantEffects, itemInfo.ReuseDelay, itemInfo.Blessed
                );
            }

            if ((mask & (int)ItemListType.SoulCrystal) != 0)
            {
                // Читаем опции души кристалла
                byte regularCount = reader.ReadByte();
                var regularOptions = new List<int>();
                for (int j = 0; j < regularCount; j++)
                {
                    regularOptions.Add(reader.ReadInt32());
                }

                byte specialCount = reader.ReadByte();
                var specialOptions = new List<int>();
                for (int j = 0; j < specialCount; j++)
                {
                    specialOptions.Add(reader.ReadInt32());
                }

                itemInfo = new ItemInfo(
                    mask, objectId, itemId, location, count, itemType2, customType1,
                    equipped, bodyPart, enchantLevel, mana, timeValue, available,
                    itemInfo.Augmentation, itemInfo.ElementalAttrs, itemInfo.VisualId,
                    regularOptions, specialOptions, itemInfo.EnchantEffects,
                    itemInfo.ReuseDelay, itemInfo.Blessed
                );
            }

            return itemInfo;
        }

        private static string? ReadNullString(BinaryReader reader)
        {
            try
            {
                // Java writeString(null) записывает только writeChar('\000') - 2 байта
                ushort charValue = reader.ReadUInt16();
                
                if (charValue == 0)
                {
                    return null; // Null строка
                }
                else
                {
                    // Читаем строку до терминатора
                    var chars = new List<char>();
                    chars.Add((char)charValue);
                    
                    int charCount = 1;
                    while (charCount <= 100) // Защита от бесконечного цикла
                    {
                        if (reader.BaseStream.Position + 2 > reader.BaseStream.Length)
                            break;
                            
                        charValue = reader.ReadUInt16();
                        if (charValue == 0)
                            break;
                            
                        chars.Add((char)charValue);
                        charCount++;
                    }
                    return new string(chars.ToArray());
                }
            }
            catch
            {
                return null;
            }
        }

        public override string ToString()
        {
            return $"ExResponseCommissionListPacket(reply_type={_replyType}, chunk_id={_chunkId}, chunk_size={_chunkSize}, items_count={_items.Count})";
        }
    }
}
