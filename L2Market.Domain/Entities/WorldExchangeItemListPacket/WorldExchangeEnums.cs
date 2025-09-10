using System;

namespace L2Market.Domain.Entities.WorldExchangeItemListPacket
{
    /// <summary>
    /// Типы предметов в World Exchange
    /// </summary>
    public enum WorldExchangeItemSubType : int
    {
        WEAPON = 1,
        ARMOR = 2,
        ACCESSORY = 3,
        MATERIAL = 4,
        CONSUMABLE = 5,
        ETC = 6,
        PET = 7,
        SKILL = 8,
        ENCHANT = 9,
        SPECIAL = 10
    }
}
