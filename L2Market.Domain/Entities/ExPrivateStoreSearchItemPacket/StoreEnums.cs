using System;

namespace L2Market.Domain.Entities.ExPrivateStoreSearchItemPacket;

public enum StoreType
{
    Sell = 0x00,
    Buy = 0x01,
    All = 0x03
}

public enum StoreItemType
{
    All = 0xFF,
    Equipment = 0x00,
    EnhancementOrExping = 0x02,
    GroceryOrCollectionMisc = 0x04
}

public enum StoreSubItemType
{
    All = 0xFF,
    Weapon = 0x00,
    Armor = 0x01,
    Accessory = 0x02,
    EquipmentMisc = 0x03,
    EnchantScroll = 0x08,
    LifeStone = 0x0F,
    Dyes = 0x10,
    Crystal = 0x11,
    Spellbook = 0x12,
    EnhancementMisc = 0x13,
    PotionScroll = 0x14,
    Ticket = 0x15,
    PackCraft = 0x16,
    GroceryMisc = 0x18
}

public enum BodyPart : long
{
    SlotNone = 0x0000,
    SlotUnderwear = 0x0001,
    SlotREar = 0x0002,
    SlotLEar = 0x0004,
    SlotLrEar = 0x0006,
    SlotNeck = 0x0008,
    SlotRFinger = 0x0010,
    SlotLFinger = 0x0020,
    SlotLrFinger = 0x0030,
    SlotHead = 0x0040,
    SlotRHand = 0x0080,
    SlotLHand = 0x0100,
    SlotGloves = 0x0200,
    SlotChest = 0x0400,
    SlotLegs = 0x0800,
    SlotFeet = 0x1000,
    SlotBack = 0x2000,
    SlotLrHand = 0x4000,
    SlotFullArmor = 0x8000,
    SlotHair = 0x010000,
    SlotAllDress = 0x020000,
    SlotHair2 = 0x040000,
    SlotHairAll = 0x080000,
    SlotRBracelet = 0x100000,
    SlotLBracelet = 0x200000,
    SlotDeco = 0x400000,
    SlotBelt = 0x10000000,
    SlotBrooch = 0x20000000,
    SlotBroochJewel = 0x40000000,
    SlotAgathion = 0x3000000000,
    SlotArtifactBook = 0x20000000000,
    SlotArtifact = 0x40000000000
}
