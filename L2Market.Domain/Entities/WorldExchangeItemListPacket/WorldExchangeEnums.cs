using System;

namespace L2Market.Domain.Entities.WorldExchangeItemListPacket
{
    /// <summary>
    /// Типы предметов в World Exchange (соответствует Java enum)
    /// </summary>
    public enum WorldExchangeItemSubType : int
    {
        WEAPON = 0,
        ARMOR = 1,
        ACCESSORY = 2,
        ETC = 3,
        ARTIFACT_B1 = 4,
        ARTIFACT_C1 = 5,
        ARTIFACT_D1 = 6,
        ARTIFACT_A1 = 7,
        ENCHANT_SCROLL = 8,
        BLESS_ENCHANT_SCROLL = 9,
        MULTI_ENCHANT_SCROLL = 10,
        ANCIENT_ENCHANT_SCROLL = 11,
        SPIRITSHOT = 12,
        SOULSHOT = 13,
        BUFF = 14,
        VARIATION_STONE = 15,
        DYE = 16,
        SOUL_CRYSTAL = 17,
        SKILLBOOK = 18,
        ETC_ENCHANT = 19,
        POTION_AND_ETC_SCROLL = 20,
        TICKET = 21,
        CRAFT = 22,
        INC_ENCHANT_PROP = 23,
        ADENA = 24,
        ETC_SUB_TYPE = 25
    }

    /// <summary>
    /// Типы сортировки в World Exchange (соответствует Java enum)
    /// </summary>
    public enum WorldExchangeSortType : int
    {
        NONE = 0,
        ITEM_NAME_ASCE = 2,
        ITEM_NAME_DESC = 3,
        PRICE_ASCE = 4,
        PRICE_DESC = 5,
        AMOUNT_ASCE = 6,
        AMOUNT_DESC = 7,
        PRICE_PER_PIECE_ASCE = 8,
        PRICE_PER_PIECE_DESC = 9
    }
}
