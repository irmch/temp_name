namespace L2Market.Domain.Entities.ExResponseCommissionListPacket
{
    /// <summary>
    /// Типы списков предметов
    /// </summary>
    public enum ItemListType
    {
        /// <summary>
        /// Бонус аугментации
        /// </summary>
        AugmentBonus = 1,
        
        /// <summary>
        /// Элементарный атрибут
        /// </summary>
        ElementalAttribute = 2,
        
        /// <summary>
        /// Визуальный ID
        /// </summary>
        VisualId = 4,
        
        /// <summary>
        /// Soul Crystal
        /// </summary>
        SoulCrystal = 8,
        
        /// <summary>
        /// Задержка повторного использования
        /// </summary>
        ReuseDelay = 16,
        
        /// <summary>
        /// Эффект заточки
        /// </summary>
        EnchantEffect = 32,
        
        /// <summary>
        /// Благословенный
        /// </summary>
        Blessed = 128
    }
}
