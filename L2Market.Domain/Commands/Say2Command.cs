using System;

namespace L2Market.Domain.Commands
{
    /// <summary>
    /// Команда для отправки сообщения в чат (SAY2)
    /// </summary>
    public class Say2Command
    {
        public byte PacketId { get; set; } = 0x49; // ID пакета SAY2
        public string Text { get; set; } = string.Empty; // Текст сообщения
        public int ChatType { get; set; } // Тип чата
        public byte Target {  get; set; }   // need to change to string. Its temp changes right now
        
        public override string ToString()
        {
            return $"Say2(Text='{Text}', Type={ChatType}, Target='{Target}')";
        }
    }
}
