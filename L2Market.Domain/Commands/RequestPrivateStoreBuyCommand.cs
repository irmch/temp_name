namespace L2Market.Domain.Commands
{
    /// <summary>
    /// Команда для покупки предмета в частном магазине
    /// </summary>
    public class RequestPrivateStoreBuyCommand
    {
        /// <summary>
        /// ID пакета
        /// </summary>
        public byte PacketId { get; set; } = 0x32;
        
        /// <summary>
        /// ID подпакета
        /// </summary>
        public ushort SubPacketId { get; set; } = 0x0001;
        
        /// <summary>
        /// ID вендора
        /// </summary>
        public int VendorObjectId { get; set; }
        
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
            return $"RequestPrivateStoreBuy(VendorId={VendorObjectId}, ItemType={ItemType}, Count={Count})";
        }
    }
}
