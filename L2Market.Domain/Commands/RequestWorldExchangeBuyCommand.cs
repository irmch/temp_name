namespace L2Market.Domain.Commands
{
    /// <summary>
    /// Команда для покупки предмета в мировом обмене
    /// </summary>
    public class RequestWorldExchangeBuyCommand
    {
        /// <summary>
        /// ID пакета
        /// </summary>
        public byte PacketId { get; set; } = 0xFE;
        
        /// <summary>
        /// ID подпакета
        /// </summary>
        public ushort SubPacketId { get; set; } = 0x0001;
        
        /// <summary>
        /// ID мирового обмена
        /// </summary>
        public int WorldExchangeId { get; set; }
        
        /// <summary>
        /// Тип предмета
        /// </summary>
        public int ItemType { get; set; }
        
        /// <summary>
        /// Количество предметов
        /// </summary>
        public long Count { get; set; } = 1;
        
        /// <summary>
        /// Строковое представление команды
        /// </summary>
        /// <returns>Строка с информацией о команде</returns>
        public override string ToString()
        {
            return $"RequestWorldExchangeBuy(WorldExchangeId={WorldExchangeId}, ItemType={ItemType}, Count={Count})";
        }
    }
}
