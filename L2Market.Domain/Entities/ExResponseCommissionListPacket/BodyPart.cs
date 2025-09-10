namespace L2Market.Domain.Entities.ExResponseCommissionListPacket
{
    /// <summary>
    /// Слоты частей тела на основе ItemTemplate.java
    /// </summary>
    public enum BodyPart : long
    {
        /// <summary>
        /// Нет слота
        /// </summary>
        SlotNone = 0x0000,
        
        /// <summary>
        /// Нижнее белье
        /// </summary>
        SlotUnderwear = 0x0001,
        
        /// <summary>
        /// Правое ухо
        /// </summary>
        SlotREar = 0x0002,
        
        /// <summary>
        /// Левое ухо
        /// </summary>
        SlotLEar = 0x0004,
        
        /// <summary>
        /// Оба уха
        /// </summary>
        SlotLREar = 0x0006,
        
        /// <summary>
        /// Шея
        /// </summary>
        SlotNeck = 0x0008,
        
        /// <summary>
        /// Правое кольцо
        /// </summary>
        SlotRFinger = 0x0010,
        
        /// <summary>
        /// Левое кольцо
        /// </summary>
        SlotLFinger = 0x0020,
        
        /// <summary>
        /// Оба кольца
        /// </summary>
        SlotLRFinger = 0x0030,
        
        /// <summary>
        /// Голова
        /// </summary>
        SlotHead = 0x0040,
        
        /// <summary>
        /// Правая рука
        /// </summary>
        SlotRHand = 0x0080,
        
        /// <summary>
        /// Левая рука
        /// </summary>
        SlotLHand = 0x0100,
        
        /// <summary>
        /// Перчатки
        /// </summary>
        SlotGloves = 0x0200,
        
        /// <summary>
        /// Грудь
        /// </summary>
        SlotChest = 0x0400,
        
        /// <summary>
        /// Ноги
        /// </summary>
        SlotLegs = 0x0800,
        
        /// <summary>
        /// Ступни
        /// </summary>
        SlotFeet = 0x1000,
        
        /// <summary>
        /// Спина
        /// </summary>
        SlotBack = 0x2000,
        
        /// <summary>
        /// Обе руки
        /// </summary>
        SlotLRHand = 0x4000,
        
        /// <summary>
        /// Полная броня
        /// </summary>
        SlotFullArmor = 0x8000,
        
        /// <summary>
        /// Волосы
        /// </summary>
        SlotHair = 0x010000,
        
        /// <summary>
        /// Все платья
        /// </summary>
        SlotAllDress = 0x020000,
        
        /// <summary>
        /// Волосы 2
        /// </summary>
        SlotHair2 = 0x040000,
        
        /// <summary>
        /// Все волосы
        /// </summary>
        SlotHairAll = 0x080000,
        
        /// <summary>
        /// Правая браслет
        /// </summary>
        SlotRBracelet = 0x100000,
        
        /// <summary>
        /// Левая браслет
        /// </summary>
        SlotLBracelet = 0x200000,
        
        /// <summary>
        /// Украшения
        /// </summary>
        SlotDeco = 0x400000,
        
        /// <summary>
        /// Пояс
        /// </summary>
        SlotBelt = 0x10000000,
        
        /// <summary>
        /// Брошь
        /// </summary>
        SlotBrooch = 0x20000000,
        
        /// <summary>
        /// Драгоценный камень броши
        /// </summary>
        SlotBroochJewel = 0x40000000,
        
        /// <summary>
        /// Агатион
        /// </summary>
        SlotAgathion = 0x3000000000,
        
        /// <summary>
        /// Книга артефактов
        /// </summary>
        SlotArtifactBook = 0x20000000000,
        
        /// <summary>
        /// Артефакт
        /// </summary>
        SlotArtifact = 0x40000000000
    }
}
