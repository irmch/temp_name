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
            // Читаем поля CommissionItem (используем unsigned для больших значений)
            ulong commissionId = reader.ReadUInt64();
            ulong pricePerUnit = reader.ReadUInt64();
            int commissionItemType = reader.ReadInt32();
            int durationType = reader.ReadInt32();
            int endTime = reader.ReadInt32();
            
            // Отладочное логирование - показываем исходную цену
            // System.Diagnostics.Debug.WriteLine($"[CommissionItem] Raw data: commissionId={commissionId} (0x{commissionId:X16}), pricePerUnit={pricePerUnit} (0x{pricePerUnit:X16}), commissionItemType={commissionItemType}, durationType={durationType}, endTime={endTime}");
            
            // Убираем ограничение на цену - пусть приходят любые значения из игры
            // if (pricePerUnit > 1000000000000000) 
            // {
            //     pricePerUnit = 0; 
            // }
            

            // Читаем имя продавца (null-terminated string)
            string? sellerName = ReadNullString(reader);
            // System.Diagnostics.Debug.WriteLine($"[CommissionItem] Seller name: '{sellerName}'");

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
            ulong count = reader.ReadUInt64(); // Используем unsigned для количества
            byte itemType2 = reader.ReadByte();
            byte customType1 = reader.ReadByte();
            ushort equipped = reader.ReadUInt16();
            ulong bodyPart = reader.ReadUInt64(); // Используем unsigned для body part
            ushort enchantLevel = reader.ReadUInt16();
            int mana = reader.ReadInt32();
            byte protocol270 = reader.ReadByte();
            int timeValue = reader.ReadInt32();
            bool available = reader.ReadByte() != 0;
            ushort locked = reader.ReadUInt16();
            
            // Отладочное логирование
            // System.Diagnostics.Debug.WriteLine($"[ItemInfo] Raw data: itemId={itemId}, count={count} (0x{count:X16}), enchantLevel={enchantLevel}, mask=0x{mask:X4}, bodyPart={bodyPart} (0x{bodyPart:X16})");
            
            // Проверяем на разумные значения
            if (itemId < 0)
            {
                // System.Diagnostics.Debug.WriteLine($"[ItemInfo] Negative itemId: {itemId}, taking absolute value");
                itemId = Math.Abs(itemId);
            }
            
            if (count > 1000000) // Максимум 1 миллион
            {
                // System.Diagnostics.Debug.WriteLine($"[ItemInfo] Count out of range: {count}, setting to 1");
                count = 1; // Минимум 1 предмет
            }
            
            if (enchantLevel > 100) // Максимум +100
            {
                // System.Diagnostics.Debug.WriteLine($"[ItemInfo] EnchantLevel out of range: {enchantLevel}, setting to 0");
                enchantLevel = 0; // Сбрасываем неразумные значения
            }

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
                
                // Читаем атрибуты защиты (6 значений по 2 байта)
                short[] defenseAttrs = new short[6];
                for (int i = 0; i < 6; i++)
                {
                    defenseAttrs[i] = reader.ReadInt16();
                }

                var elementalAttrs = new Dictionary<string, int>
                {
                    ["attack_type"] = attackType,
                    ["attack_power"] = attackPower,
                    ["defense_fire"] = defenseAttrs[0],
                    ["defense_water"] = defenseAttrs[1],
                    ["defense_wind"] = defenseAttrs[2],
                    ["defense_earth"] = defenseAttrs[3],
                    ["defense_holy"] = defenseAttrs[4],
                    ["defense_unholy"] = defenseAttrs[5]
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
                // Проверяем, есть ли данные для чтения
                if (reader.BaseStream.Position + 2 > reader.BaseStream.Length)
                    return null;
                
                // Java writeString(null) записывает только writeChar('\000') - 2 байта
                ushort charValue = reader.ReadUInt16();
                
                if (charValue == 0)
                {
                    return null; // Null строка
                }
                else
                {
                    // Читаем строку до терминатора (UTF-16LE)
                    var chars = new List<char>();
                    chars.Add((char)charValue);
                    
                    int charCount = 1;
                    while (charCount <= 100) // Защита от бесконечного цикла
                    {
                        if (reader.BaseStream.Position + 2 > reader.BaseStream.Length)
                            break;
                            
                        ushort nextChar = reader.ReadUInt16();
                        if (nextChar == 0)
                            break;
                            
                        chars.Add((char)nextChar);
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
