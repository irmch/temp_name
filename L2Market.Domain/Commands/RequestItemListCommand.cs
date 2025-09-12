using System;

namespace L2Market.Domain.Commands
{
    /// <summary>
    /// Команда для запроса списка предметов в частных магазинах (Private Stores)
    /// </summary>
    public class RequestItemListCommand
    {
        /// <summary>
        /// ID пакета
        /// </summary>
        public byte PacketId { get; set; } = 0xD0;
        
        /// <summary>
        /// ID подпакета
        /// </summary>
        public ushort SubPacketId { get; set; } = 0x0214;
        
        /// <summary>
        /// Неизвестное поле 2
        /// </summary>
        public ushort Unknown2 { get; set; } = 0x0000;
        
        /// <summary>
        /// Неизвестное поле 3
        /// </summary>
        public byte Unknown3 { get; set; } = 0x03;
        
        /// <summary>
        /// Тип предмета (0-4)
        /// </summary>
        public byte ItemType { get; set; } = 0x00;
        
        /// <summary>
        /// Неизвестное поле 4
        /// </summary>
        public ushort Unknown4 { get; set; } = 0x00FF;
        
        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        public RequestItemListCommand()
        {
        }
        
        /// <summary>
        /// Конструктор с параметром для ItemType
        /// </summary>
        /// <param name="itemType">Тип предмета (0-4)</param>
        public RequestItemListCommand(byte itemType)
        {
            ItemType = itemType;
        }
        
        /// <summary>
        /// Строковое представление команды
        /// </summary>
        /// <returns>Строка с информацией о команде</returns>
        public override string ToString()
        {
            return $"RequestItemList(ItemType={ItemType})";
        }
    }
}
