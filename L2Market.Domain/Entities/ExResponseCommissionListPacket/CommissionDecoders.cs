using System;
using System.Linq;
using System.Collections.Generic; // Added missing import

namespace L2Market.Domain.Entities.ExResponseCommissionListPacket
{
    /// <summary>
    /// Вспомогательные методы для декодирования значений комиссии
    /// </summary>
    public static class CommissionDecoders
    {
        /// <summary>
        /// Декодирует body part в читаемое название
        /// </summary>
        public static string DecodeBodyPart(long bodyPart)
        {
            try
            {
                // Проверяем точные совпадения
                foreach (var bp in Enum.GetValues<BodyPart>().Cast<BodyPart>())
                {
                    if (bp == (BodyPart)bodyPart)
                    {
                        return bp.ToString();
                    }
                }

                // Проверяем комбинации (несколько слотов)
                var parts = new List<string>();
                long remaining = bodyPart;

                // Проверяем все флаги от большего к меньшему
                foreach (var bp in Enum.GetValues<BodyPart>().Cast<BodyPart>().OrderByDescending(x => (long)x))
                {
                    if ((long)bp != 0 && (remaining & (long)bp) == (long)bp)
                    {
                        parts.Add(bp.ToString());
                        remaining &= ~(long)bp;
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

        /// <summary>
        /// Декодирует поле mana
        /// </summary>
        public static string DecodeMana(int mana)
        {
            if (mana == -1)
                return "no mana/not shadow item";
            else if (mana == 0)
                return "shadow item expired";
            else if (mana > 0)
                return $"shadow item time: {mana} seconds";
            else
                return "unknown mana value";
        }

        /// <summary>
        /// Декодирует поле time
        /// </summary>
        public static string DecodeTime(int timeValue)
        {
            if (timeValue == -9999)
                return "not time-limited";
            else if (timeValue == -2559744)
                return "special time value (possibly display bug)";
            else if (timeValue > 0)
            {
                int hours = timeValue / 3600;
                int minutes = (timeValue % 3600) / 60;
                int seconds = timeValue % 60;
                return $"time-limited: {hours}h {minutes}m {seconds}s remaining";
            }
            else
                return "unknown time value";
        }

        /// <summary>
        /// Декодирует item type2
        /// </summary>
        public static string DecodeItemType2(int type2)
        {
            var type2Names = new Dictionary<int, string>
            {
                { 0, "WEAPON" },
                { 1, "SHIELD_ARMOR" },
                { 2, "ACCESSORY" },
                { 3, "QUEST" },
                { 4, "MONEY" },
                { 5, "OTHER" }
            };

            return type2Names.TryGetValue(type2, out string? name) ? name : $"UNKNOWN_TYPE2({type2})";
        }

        /// <summary>
        /// Декодирует custom type1 (обычно всегда 0)
        /// </summary>
        public static string DecodeCustomType1(int customType1)
        {
            return customType1 == 0 ? "normal" : $"custom({customType1})";
        }

        /// <summary>
        /// Декодирует equipped статус
        /// </summary>
        public static string DecodeEquipped(int equipped)
        {
            if (equipped == 0)
                return "not equipped";
            else if (equipped == 1)
                return "equipped";
            else
                return $"unknown equipped({equipped})";
        }

        /// <summary>
        /// Форматирует timestamp в читаемый вид
        /// </summary>
        public static string FormatTimestamp(int timestamp)
        {
            try
            {
                var dt = DateTimeOffset.FromUnixTimeSeconds(timestamp);
                return dt.ToString("dd.MM.yyyy HH:mm");
            }
            catch
            {
                return $"timestamp({timestamp})";
            }
        }
    }
}
