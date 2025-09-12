namespace L2Market.Domain.Commands
{
    /// <summary>
    /// Команда для запроса информации о покупке предмета в комиссии
    /// </summary>
    public class RequestCommissionBuyInfoCommand
    {
        /// <summary>
        /// ID пакета
        /// </summary>
        public byte PacketId { get; set; } = 0xD0;
        
        /// <summary>
        /// ID подпакета
        /// </summary>
        public ushort SubPacketId { get; set; } = 0x009E;
        
        /// <summary>
        /// Неизвестное поле 1
        /// </summary>
        public int Unknown1 { get; set; } = 0x00000001; // 01 00 00 00
        
        /// <summary>
        /// Тип предмета
        /// </summary>
        public int ItemType { get; set; } // 05 00 00 00 (меняется)
        
        /// <summary>
        /// Неизвестное поле 2
        /// </summary>
        public long Unknown2 { get; set; } = -1; // FF FF FF FF FF FF FF FF
        
        /// <summary>
        /// Неизвестное поле 3
        /// </summary>
        public ushort Unknown3 { get; set; } = 0x0000; // 00 00
        
        /// <summary>
        /// Строковое представление команды
        /// </summary>
        /// <returns>Строка с информацией о команде</returns>
        public override string ToString()
        {
            return $"RequestCommissionBuyInfo(ItemType={ItemType})";
        }
    }
}
