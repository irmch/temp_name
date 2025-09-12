using System;

namespace L2Market.Domain.Commands
{
    /// <summary>
    /// Команда для вызова окна приватных магазинов
    /// </summary>
    public class RequestPrivateStoreWindowCommand
    {
        /// <summary>
        /// ID пакета
        /// </summary>
        public byte PacketId { get; set; } = 0xD0;
        
        /// <summary>
        /// ID подпакета
        /// </summary>
        public byte SubPacketId { get; set; } = 0x15;
        
        /// <summary>
        /// Неизвестное поле
        /// </summary>
        public byte Unknown { get; set; } = 0x02;
        
        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        public RequestPrivateStoreWindowCommand()
        {
        }
        
        /// <summary>
        /// Строковое представление команды
        /// </summary>
        /// <returns>Строка с информацией о команде</returns>
        public override string ToString()
        {
            return "RequestPrivateStoreWindow()";
        }
    }
}
