namespace L2Market.Domain.Entities.ExResponseCommissionListPacket
{
    /// <summary>
    /// Типы ответов для списка комиссий
    /// </summary>
    public enum CommissionListReplyType
    {
        /// <summary>
        /// Аукционы игрока пусты
        /// </summary>
        PlayerAuctionsEmpty = -2,
        
        /// <summary>
        /// Предмет не существует
        /// </summary>
        ItemDoesNotExist = -1,
        
        /// <summary>
        /// Аукционы игрока
        /// </summary>
        PlayerAuctions = 2,
        
        /// <summary>
        /// Все аукционы
        /// </summary>
        Auctions = 3
    }
}
