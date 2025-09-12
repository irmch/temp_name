using System;

namespace L2Market.Domain.Commands
{
    /// <summary>
    /// Команда для выполнения действия с объектом (клик по NPC, предмету и т.д.)
    /// </summary>
    public class ActionCommand
    {
        /// <summary>
        /// ID пакета
        /// </summary>
        public byte PacketId { get; set; } = 0x1F;
        
        /// <summary>
        /// ID объекта (NPC, игрок, предмет и т.д.)
        /// </summary>
        public int ObjectId { get; set; }
        
        /// <summary>
        /// X координата игрока
        /// </summary>
        public int OriginX { get; set; }
        
        /// <summary>
        /// Y координата игрока
        /// </summary>
        public int OriginY { get; set; }
        
        /// <summary>
        /// Z координата игрока
        /// </summary>
        public int OriginZ { get; set; }
        
        /// <summary>
        /// Тип действия: 0 - Simple click, 1 - Shift click
        /// </summary>
        public byte ActionId { get; set; } = 0;
        
        /// <summary>
        /// Конструктор по умолчанию
        /// </summary>
        public ActionCommand()
        {
        }
        
        /// <summary>
        /// Конструктор с параметрами
        /// </summary>
        /// <param name="objectId">ID объекта</param>
        /// <param name="originX">X координата</param>
        /// <param name="originY">Y координата</param>
        /// <param name="originZ">Z координата</param>
        /// <param name="actionId">Тип действия (0 или 1)</param>
        public ActionCommand(int objectId, int originX, int originY, int originZ, byte actionId = 0)
        {
            ObjectId = objectId;
            OriginX = originX;
            OriginY = originY;
            OriginZ = originZ;
            ActionId = actionId;
        }
        
        /// <summary>
        /// Строковое представление команды
        /// </summary>
        /// <returns>Строка с информацией о команде</returns>
        public override string ToString()
        {
            return $"Action(ObjectId={ObjectId}, Pos=({OriginX},{OriginY},{OriginZ}), ActionId={ActionId})";
        }
    }
}
