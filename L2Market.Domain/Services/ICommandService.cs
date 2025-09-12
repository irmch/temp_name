using System.Threading.Tasks;
using L2Market.Domain.Commands;

namespace L2Market.Domain.Services
{
    /// <summary>
    /// Интерфейс для отправки команд через NamedPipe
    /// </summary>
    public interface ICommandService
    {
        /// <summary>
        /// Отправляет команду покупки предмета в комиссии
        /// </summary>
        /// <param name="itemType">Тип предмета</param>
        /// <returns>Task</returns>
        Task SendCommissionBuyCommandAsync(int itemType);
        
        /// <summary>
        /// Отправляет команду покупки предмета в частном магазине
        /// </summary>
        /// <param name="itemType">Тип предмета</param>
        /// <param name="vendorObjectId">ID вендора</param>
        /// <returns>Task</returns>
        Task SendPrivateStoreBuyCommandAsync(int itemType, int vendorObjectId);
        
        /// <summary>
        /// Отправляет команду покупки предмета в мировом обмене
        /// </summary>
        /// <param name="itemType">Тип предмета</param>
        /// <param name="worldExchangeId">ID мирового обмена</param>
        /// <returns>Task</returns>
        Task SendWorldExchangeBuyCommandAsync(int itemType, int worldExchangeId);
        
        /// <summary>
        /// Отправляет сообщение в чат (SAY2)
        /// </summary>
        /// <param name="text">Текст сообщения</param>
        /// <param name="chatType">Тип чата (по умолчанию 0)</param>
        /// <param name="target">Цель для whisper (опционально)</param>
        /// <returns>Task</returns>
        Task SendSay2CommandAsync(string text, int chatType = 0, string? target = null);
        
        /// <summary>
        /// Отправляет bypass команду к серверу
        /// </summary>
        /// <param name="npcId">ID NPC</param>
        /// <param name="action">Действие для выполнения</param>
        /// <returns>Task</returns>
        Task SendBypassCommandAsync(string npcId, string action);
        
        /// <summary>
        /// Отправляет команду действия с объектом
        /// </summary>
        /// <param name="objectId">ID объекта</param>
        /// <param name="originX">X координата игрока</param>
        /// <param name="originY">Y координата игрока</param>
        /// <param name="originZ">Z координата игрока</param>
        /// <param name="actionId">Тип действия (0 или 1)</param>
        /// <returns>Task</returns>
        Task SendActionCommandAsync(int objectId, int originX, int originY, int originZ, byte actionId = 0);
        
        /// <summary>
        /// Отправляет команду запроса списка предметов в частных магазинах
        /// </summary>
        /// <param name="itemType">Тип предмета (0-4)</param>
        /// <returns>Task</returns>
        Task SendItemListCommandAsync(byte itemType = 0);
        
        /// <summary>
        /// Отправляет команду вызова окна приватных магазинов
        /// </summary>
        /// <returns>Task</returns>
        Task SendPrivateStoreWindowCommandAsync();
        
        /// <summary>
        /// Отправляет команду поиска предметов во всемирном магазине
        /// </summary>
        /// <param name="itemType">Тип предмета/категория (0x00-0x19)</param>
        /// <param name="unknown2">Значение для Unknown2 (по умолчанию 0x0002)</param>
        /// <returns>Task</returns>
        Task SendWorldExchangeSearchCommandAsync(byte itemType = 0x00, ushort unknown2 = 0x0002);
    }
}
