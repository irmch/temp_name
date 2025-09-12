using System.Collections.Generic;
using System.IO;
using System.Text;
using System;

namespace L2Market.Domain.Entities.ExPrivateStoreSearchItemPacket;

public class ExPrivateStoreSearchItemPacket
{
    public const int MaxItemPerPage = 120;

    private readonly int _page;
    private readonly int _maxPage;
    private readonly int _nSize;
    private readonly List<PrivateStoreItem> _items;

    public int Page => _page;
    public int MaxPage => _maxPage;
    public int NSize => _nSize;
    public IReadOnlyList<PrivateStoreItem> Items => _items.AsReadOnly();

    public ExPrivateStoreSearchItemPacket(
        int page,
        int maxPage,
        int nSize,
        List<PrivateStoreItem> items)
    {
        _page = page;
        _maxPage = maxPage;
        _nSize = nSize;
        _items = items ?? new List<PrivateStoreItem>();
    }

    public static ExPrivateStoreSearchItemPacket FromBytes(byte[] data)
    {
        using var stream = new MemoryStream(data);
        using var reader = new BinaryReader(stream);

        // Читаем основные поля пакета (данные уже без заголовка)
        int page = reader.ReadByte();
        int maxPage = reader.ReadByte();
        int nSize = reader.ReadInt32();

        var items = new List<PrivateStoreItem>();

        // Читаем предметы если есть
        if (nSize > 0)
        {
            for (int i = 0; i < nSize; i++)
            {
                try
                {
                    var item = ReadPrivateStoreItem(reader);
                    items.Add(item);
                }
                catch (Exception ex)
                {
                    // Логируем ошибку и продолжаем чтение
                    System.Diagnostics.Debug.WriteLine($"Error reading item {i + 1}/{nSize}: {ex.Message}");
                    break;
                }
            }
        }

        return new ExPrivateStoreSearchItemPacket(page, maxPage, nSize, items);
    }

    private static PrivateStoreItem ReadPrivateStoreItem(BinaryReader reader)
    {
        // Читаем поля PrivateStoreItem
        string vendorName = ReadSizedStringUtf16(reader);
        int vendorObjectId = reader.ReadInt32();
        int storeType = reader.ReadByte();
        long price = reader.ReadInt64();
        int vendorX = reader.ReadInt32();
        int vendorY = reader.ReadInt32();
        int vendorZ = reader.ReadInt32();

        // Читаем размер данных предмета
        int itemSize = reader.ReadInt32();

        // Создаем подпоток для ItemInfo с ограниченным размером
        byte[] itemData = reader.ReadBytes(itemSize);
        using var itemStream = new MemoryStream(itemData);
        using var itemReader = new BinaryReader(itemStream);

        // Читаем ItemInfo
        var itemInfo = ReadItemInfo(itemReader, itemSize);

        return new PrivateStoreItem(
            vendorName,
            vendorObjectId,
            storeType,
            price,
            vendorX,
            vendorY,
            vendorZ,
            itemInfo);
    }

    private static ItemInfo ReadItemInfo(BinaryReader reader, int itemSize)
    {
        long startPosition = reader.BaseStream.Position;
        long streamLength = reader.BaseStream.Length;

        // Читаем основные поля предмета
        int mask = reader.ReadUInt16();
        int objectId = reader.ReadInt32();
        int itemId = reader.ReadInt32();
        int location = reader.ReadByte();
        long count = reader.ReadInt64();
        int itemType2 = reader.ReadByte();
        int customType1 = reader.ReadByte();
        int equipped = reader.ReadUInt16();
        long bodyPart = (long)reader.ReadUInt64();
        int enchantLevel = reader.ReadUInt16();
        int mana = reader.ReadInt32();
        int protocol270 = reader.ReadByte();
        int timeValue = reader.ReadInt32();
        bool available = reader.ReadByte() != 0;
        int locked = reader.ReadUInt16();

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
            available);

        // Читаем опциональные поля на основе маски
        if ((mask & ItemListType.AugmentBonus) != 0)
        {
            int option1 = reader.ReadInt32();
            int option2 = reader.ReadInt32();
            var augmentation = new Dictionary<string, int>
            {
                ["option1"] = option1,
                ["option2"] = option2
            };
            
            // Создаем новый ItemInfo с аугментацией
            itemInfo = new ItemInfo(
                itemInfo.Mask,
                itemInfo.ObjectId,
                itemInfo.ItemId,
                itemInfo.Location,
                itemInfo.Count,
                itemInfo.ItemType2,
                itemInfo.CustomType1,
                itemInfo.Equipped,
                itemInfo.BodyPart,
                itemInfo.EnchantLevel,
                itemInfo.Mana,
                itemInfo.Time,
                itemInfo.Available,
                augmentation);
        }

        if ((mask & ItemListType.ElementalAttribute) != 0)
        {
            short attackType = reader.ReadInt16();
            short attackPower = reader.ReadInt16();

            // Читаем защиты (6 значений по 2 байта)
            var defenses = new List<short>();
            for (int i = 0; i < 6; i++)
            {
                defenses.Add(reader.ReadInt16());
            }

            var elementalAttrs = new Dictionary<string, int>
            {
                ["attack_type"] = attackType,
                ["attack_power"] = attackPower,
                ["defense_fire"] = defenses[0],
                ["defense_water"] = defenses[1],
                ["defense_wind"] = defenses[2],
                ["defense_earth"] = defenses[3],
                ["defense_holy"] = defenses[4],
                ["defense_dark"] = defenses[5]
            };

            // Создаем новый ItemInfo с элементарными атрибутами
            itemInfo = new ItemInfo(
                itemInfo.Mask,
                itemInfo.ObjectId,
                itemInfo.ItemId,
                itemInfo.Location,
                itemInfo.Count,
                itemInfo.ItemType2,
                itemInfo.CustomType1,
                itemInfo.Equipped,
                itemInfo.BodyPart,
                itemInfo.EnchantLevel,
                itemInfo.Mana,
                itemInfo.Time,
                itemInfo.Available,
                itemInfo.Augmentation,
                elementalAttrs);
        }

        if ((mask & ItemListType.VisualId) != 0)
        {
            int visualId = reader.ReadInt32();

            // Создаем новый ItemInfo с visual ID
            itemInfo = new ItemInfo(
                itemInfo.Mask,
                itemInfo.ObjectId,
                itemInfo.ItemId,
                itemInfo.Location,
                itemInfo.Count,
                itemInfo.ItemType2,
                itemInfo.CustomType1,
                itemInfo.Equipped,
                itemInfo.BodyPart,
                itemInfo.EnchantLevel,
                itemInfo.Mana,
                itemInfo.Time,
                itemInfo.Available,
                itemInfo.Augmentation,
                itemInfo.ElementalAttrs,
                visualId);
        }

        if ((mask & ItemListType.SoulCrystal) != 0)
        {
            // Читаем опции души кристалла
            byte regularCount = reader.ReadByte();
            var regularOptions = new List<int>();
            for (int j = 0; j < regularCount; j++)
            {
                int optionId = reader.ReadInt32();
                regularOptions.Add(optionId);
            }

            byte specialCount = reader.ReadByte();
            var specialOptions = new List<int>();
            for (int j = 0; j < specialCount; j++)
            {
                int optionId = reader.ReadInt32();
                specialOptions.Add(optionId);
            }

            // Создаем новый ItemInfo с soul crystal опциями
            itemInfo = new ItemInfo(
                itemInfo.Mask,
                itemInfo.ObjectId,
                itemInfo.ItemId,
                itemInfo.Location,
                itemInfo.Count,
                itemInfo.ItemType2,
                itemInfo.CustomType1,
                itemInfo.Equipped,
                itemInfo.BodyPart,
                itemInfo.EnchantLevel,
                itemInfo.Mana,
                itemInfo.Time,
                itemInfo.Available,
                itemInfo.Augmentation,
                itemInfo.ElementalAttrs,
                itemInfo.VisualId,
                regularOptions,
                specialOptions);
        }

        if ((mask & ItemListType.ReuseDelay) != 0)
        {
            int reuseDelay = reader.ReadInt32();

            // Создаем новый ItemInfo с reuse delay
            itemInfo = new ItemInfo(
                itemInfo.Mask,
                itemInfo.ObjectId,
                itemInfo.ItemId,
                itemInfo.Location,
                itemInfo.Count,
                itemInfo.ItemType2,
                itemInfo.CustomType1,
                itemInfo.Equipped,
                itemInfo.BodyPart,
                itemInfo.EnchantLevel,
                itemInfo.Mana,
                itemInfo.Time,
                itemInfo.Available,
                itemInfo.Augmentation,
                itemInfo.ElementalAttrs,
                itemInfo.VisualId,
                itemInfo.SoulCrystalOptions,
                itemInfo.SoulCrystalSpecialOptions,
                itemInfo.EnchantEffects,
                reuseDelay,
                itemInfo.Blessed);
        }

        if ((mask & ItemListType.EnchantEffect) != 0)
        {
            // Читаем количество эффектов зачарования
            byte effectCount = reader.ReadByte();
            System.Diagnostics.Debug.WriteLine($"[ReadItemInfo] EnchantEffect mask found, effectCount: {effectCount}");
            var enchantEffects = new List<int>();
            for (int i = 0; i < effectCount; i++)
            {
                int effect = reader.ReadInt32();
                enchantEffects.Add(effect);
                System.Diagnostics.Debug.WriteLine($"[ReadItemInfo] EnchantEffect {i}: {effect}");
            }

            // Создаем новый ItemInfo с эффектами зачарования
            itemInfo = new ItemInfo(
                itemInfo.Mask,
                itemInfo.ObjectId,
                itemInfo.ItemId,
                itemInfo.Location,
                itemInfo.Count,
                itemInfo.ItemType2,
                itemInfo.CustomType1,
                itemInfo.Equipped,
                itemInfo.BodyPart,
                itemInfo.EnchantLevel,
                itemInfo.Mana,
                itemInfo.Time,
                itemInfo.Available,
                itemInfo.Augmentation,
                itemInfo.ElementalAttrs,
                itemInfo.VisualId,
                itemInfo.SoulCrystalOptions,
                itemInfo.SoulCrystalSpecialOptions,
                enchantEffects,
                itemInfo.ReuseDelay,
                itemInfo.Blessed);
        }

        if ((mask & ItemListType.Blessed) != 0)
        {
            bool blessed = reader.ReadByte() != 0;

            // Создаем новый ItemInfo с blessed статусом
            itemInfo = new ItemInfo(
                itemInfo.Mask,
                itemInfo.ObjectId,
                itemInfo.ItemId,
                itemInfo.Location,
                itemInfo.Count,
                itemInfo.ItemType2,
                itemInfo.CustomType1,
                itemInfo.Equipped,
                itemInfo.BodyPart,
                itemInfo.EnchantLevel,
                itemInfo.Mana,
                itemInfo.Time,
                itemInfo.Available,
                itemInfo.Augmentation,
                itemInfo.ElementalAttrs,
                itemInfo.VisualId,
                itemInfo.SoulCrystalOptions,
                itemInfo.SoulCrystalSpecialOptions,
                itemInfo.EnchantEffects,
                itemInfo.ReuseDelay,
                blessed);
        }

        // Проверяем, что мы не вышли за пределы размера
        long currentPosition = reader.BaseStream.Position;
        long readBytes = currentPosition - startPosition;
        if (readBytes > itemSize)
        {
            throw new InvalidDataException($"Read more bytes than expected: {readBytes} > {itemSize}");
        }
        
        // Проверяем, что мы прочитали все данные ItemInfo
        if (currentPosition != streamLength)
        {
            // Это предупреждение, а не ошибка - некоторые поля могут отсутствовать
            // System.Diagnostics.Debug.WriteLine($"Warning: read {currentPosition} of {streamLength} bytes in ItemInfo");
        }

        return itemInfo;
    }

    private static string ReadSizedStringUtf16(BinaryReader reader)
    {
        // Читаем размер строки (ushort)
        ushort stringLength = reader.ReadUInt16();
        
        if (stringLength == 0)
            return string.Empty;

        // Читаем строку (UTF-16LE)
        byte[] stringBytes = reader.ReadBytes(stringLength * 2);
        return Encoding.Unicode.GetString(stringBytes);
    }

    public override string ToString()
    {
        return $"ExPrivateStoreSearchItemPacket(page={_page}/{_maxPage}, items_count={_items.Count})";
    }
}

// Константы для типов элементов
public static class ItemListType
{
    public const int AugmentBonus = 1;
    public const int ElementalAttribute = 2;
    public const int VisualId = 4;
    public const int SoulCrystal = 8;
    public const int ReuseDelay = 16;
    public const int EnchantEffect = 32;
    public const int Blessed = 128;
}
