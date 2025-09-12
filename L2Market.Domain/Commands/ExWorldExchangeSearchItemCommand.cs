using System;

namespace L2Market.Domain.Commands
{
    /// <summary>
    /// Команда для поиска предметов во всемирном магазине (World Exchange)
    /// </summary>
    public class ExWorldExchangeSearchItemCommand
    {
        /// <summary>
        /// ID пакета
        /// </summary>
        public byte PacketId { get; set; } = 0xD0;
        
        /// <summary>
        /// ID подпакета
        /// </summary>
        public ushort SubPacketId { get; set; } = 0x023F;
        
        
        /// <summary>
        /// Тип предмета/категория (0x00-0x19)
        /// </summary>
        public ushort ItemType { get; set; } = 0x00;
        
        /// <summary>
        /// Неизвестное поле 2
        /// </summary>
        public ushort Unknown2 { get; set; } = 0x0002;
        
        /// <summary>
        /// Неизвестное поле 3
        /// </summary>
        public uint Unknown3 { get; set; } = 0x00000000;
        
        /// <summary>
        /// Неизвестное поле 4
        /// </summary>
        public ushort Unknown4 { get; set; } = 0x00000000;
        
        /// <summary>
        /// Неизвестное поле 5
        /// </summary>
        public byte Unknown5 { get; set; } = 0x00;
        
        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        public ExWorldExchangeSearchItemCommand()
        {
        }
        
        /// <summary>
        /// Конструктор с параметром для ItemType
        /// </summary>
        /// <param name="itemType">Тип предмета/категория (0x00-0x19)</param>
        public ExWorldExchangeSearchItemCommand(byte itemType)
        {
            ItemType = itemType;
        }
        
        /// <summary>
        /// Конструктор с полными параметрами
        /// </summary>
        /// <param name="itemType">Тип предмета/категория</param>
        /// <param name="unknown2">Значение для Unknown2</param>
        public ExWorldExchangeSearchItemCommand(byte itemType, ushort unknown2)
        {
            ItemType = itemType;
            Unknown2 = unknown2;
        }
        
        /// <summary>
        /// Строковое представление команды
        /// </summary>
        /// <returns>Строка с информацией о команде</returns>
        public override string ToString()
        {
            return $"ExWorldExchangeSearchItem(ItemType=0x{ItemType:X2})";
        }
    }
}
