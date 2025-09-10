using System.Collections.Generic;
using System;
using System.Linq;

namespace L2Market.Domain.Entities.ExPrivateStoreSearchItemPacket;

public static class StoreDecoders
{
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

    public static string DecodeBodyPart(long bodyPart)
    {
        try
        {
            // Проверяем точные совпадения
            var bodyPartValues = Enum.GetValues<BodyPart>().Cast<BodyPart>();
            foreach (BodyPart bp in bodyPartValues)
            {
                if ((long)bp == bodyPart)
                {
                    return bp.ToString();
                }
            }

            // Проверяем комбинации (несколько слотов)
            var parts = new List<string>();
            long remaining = bodyPart;

            // Проверяем все флаги от большего к меньшему
            var sortedBodyParts = bodyPartValues
                .OrderByDescending(x => (long)x)
                .ToList();

            foreach (var bp in sortedBodyParts)
            {
                long bpValue = (long)bp;
                if (bpValue != 0 && (remaining & bpValue) == bpValue)
                {
                    parts.Add(bp.ToString());
                    remaining &= ~bpValue;
                }
            }

            if (parts.Any())
            {
                return string.Join(" | ", parts);
            }
            else
            {
                return $"UNKNOWN_SLOT(0x{bodyPart:X})";
            }
        }
        catch
        {
            return $"ERROR_DECODING(0x{bodyPart:X})";
        }
    }

    public static string DecodeItemType2(int type2)
    {
        return type2 switch
        {
            0 => "WEAPON",
            1 => "SHIELD_ARMOR",
            2 => "ACCESSORY",
            3 => "QUEST",
            4 => "MONEY",
            5 => "OTHER",
            _ => $"UNKNOWN_TYPE2({type2})"
        };
    }

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

    public static string[] GetElementNames()
    {
        return new[] { "Огонь", "Вода", "Ветер", "Земля", "Свет", "Тьма" };
    }
}
