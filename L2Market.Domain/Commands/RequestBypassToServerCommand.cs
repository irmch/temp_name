using System;

namespace L2Market.Domain.Commands
{
    /// <summary>
    /// Команда для отправки bypass запроса к серверу
    /// </summary>
    public class RequestBypassToServerCommand
    {
        /// <summary>
        /// ID пакета
        /// </summary>
        public byte PacketId { get; set; } = 0x23;
        
        /// <summary>
        /// Полная команда bypass
        /// </summary>
        public string Command => $"menu_select?ask=-10303&reply=1";
        
        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        public RequestBypassToServerCommand()
        {
        }

        /// <summary>
        /// Строковое представление команды
        /// </summary>
        /// <returns>Строка с информацией о команде</returns>
        public override string ToString()
        {
            return $"RequestBypassToServer(Command={Command})";
        }
    }
}