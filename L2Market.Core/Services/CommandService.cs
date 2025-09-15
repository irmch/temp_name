using System;
using System.Text;
using System.Threading.Tasks;
using L2Market.Domain.Commands;
using L2Market.Domain.Services;
using L2Market.Domain.Utils;
using L2Market.Domain.Common;
using L2Market.Domain.Events;
using Microsoft.Extensions.Logging;

namespace L2Market.Core.Services
{
    /// <summary>
    /// Сервис для отправки команд через NamedPipe
    /// </summary>
    public class CommandService : ICommandService, IDisposable
    {
        private readonly INamedPipeService _pipeService;
        private readonly ILocalEventBus _eventBus;
        private readonly ILogger<CommandService> _logger;
        private readonly uint _processId;

        public CommandService(
            INamedPipeService pipeService,
            ILocalEventBus eventBus,
            ILogger<CommandService> logger,
            uint processId = 0)
        {
            _pipeService = pipeService ?? throw new ArgumentNullException(nameof(pipeService));
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _processId = processId;
        }


        /// <summary>
        /// Отправляет команду покупки предмета в комиссии
        /// </summary>
        public async Task SendCommissionBuyCommandAsync(int itemType)
        {
            try
            {
                _logger.LogInformation("[CommandService] Sending commission buy command for item type: {ItemType}", itemType);
                
                var cmd = new RequestCommissionBuyInfoCommand
                {
                    ItemType = itemType
                };

                string hex = PacketHexHelper.ToHex(cmd, Encoding.UTF8, true);
                
                // Логируем данные после преобразования
                await _eventBus.PublishAsync(new LogMessageReceivedEvent(
                    $"[CommandService] Commission buy command hex: {hex}"));
                
                await _pipeService.SendCommandAsync(hex, _processId);
                await _eventBus.PublishAsync(new LogMessageReceivedEvent(
                    $"[CommandService] Commission buy command sent successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CommandService] Error sending commission buy command for item type: {ItemType}", itemType);
                await _eventBus.PublishAsync(new LogMessageReceivedEvent(
                    $"[CommandService] Error sending commission buy command: {ex.Message}"));
            }
        }

        /// <summary>
        /// Отправляет команду покупки предмета в частном магазине
        /// </summary>
        public async Task SendPrivateStoreBuyCommandAsync(int itemType, int vendorObjectId)
        {
            try
            {
                _logger.LogInformation("[CommandService] Sending private store buy command for item type: {ItemType}, vendor: {VendorId}", 
                    itemType, vendorObjectId);
                
                var cmd = new RequestPrivateStoreBuyCommand
                {
                    ItemType = itemType,
                    VendorObjectId = vendorObjectId
                };

                string hex = PacketHexHelper.ToHex(cmd, Encoding.UTF8, true);
                
                // Логируем данные после преобразования
                await _eventBus.PublishAsync(new LogMessageReceivedEvent(
                    $"[CommandService] Private store buy command hex: {hex}"));
                
                await _pipeService.SendCommandAsync(hex, _processId);
                await _eventBus.PublishAsync(new LogMessageReceivedEvent(
                    $"[CommandService] Private store buy command sent successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CommandService] Error sending private store buy command for item type: {ItemType}, vendor: {VendorId}", 
                    itemType, vendorObjectId);
                await _eventBus.PublishAsync(new LogMessageReceivedEvent(
                    $"[CommandService] Error sending private store buy command: {ex.Message}"));
            }
        }

        /// <summary>
        /// Отправляет команду покупки предмета в мировом обмене
        /// </summary>
        public async Task SendWorldExchangeBuyCommandAsync(int itemType, int worldExchangeId)
        {
            try
            {
                _logger.LogInformation("[CommandService] Sending world exchange buy command for item type: {ItemType}, exchange: {ExchangeId}", 
                    itemType, worldExchangeId);
                
                var cmd = new RequestWorldExchangeBuyCommand
                {
                    ItemType = itemType,
                    WorldExchangeId = worldExchangeId
                };

                string hex = PacketHexHelper.ToHex(cmd, Encoding.UTF8, true);
                
                // Логируем данные после преобразования
                await _eventBus.PublishAsync(new LogMessageReceivedEvent(
                    $"[CommandService] World exchange buy command hex: {hex}"));
                
                await _pipeService.SendCommandAsync(hex, _processId);
                await _eventBus.PublishAsync(new LogMessageReceivedEvent(
                    $"[CommandService] World exchange buy command sent successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CommandService] Error sending world exchange buy command for item type: {ItemType}, exchange: {ExchangeId}", 
                    itemType, worldExchangeId);
                await _eventBus.PublishAsync(new LogMessageReceivedEvent(
                    $"[CommandService] Error sending world exchange buy command: {ex.Message}"));
            }
        }

        /// <summary>
        /// Отправляет сообщение в чат (SAY2)
        /// </summary>
        public async Task SendSay2CommandAsync(string text, int chatType = 0, string? target = null)
        {
            try
            {
                _logger.LogInformation("[CommandService] Sending SAY2 command: Text='{Text}', ChatType={ChatType}, Target='{Target}'", 
                    text, chatType, target);
                
                var cmd = new Say2Command
                {
                    Text = text ?? string.Empty,
                    ChatType = chatType,
                    Target = 0
                };

                string hex = PacketHexHelper.ToHexUtf16Le(cmd, false);
                
                // Логируем данные после преобразования
                await _eventBus.PublishAsync(new LogMessageReceivedEvent(
                    $"[CommandService] SAY2 command hex: {hex}"));
                
                await _pipeService.SendCommandAsync(hex, _processId);
                await _eventBus.PublishAsync(new LogMessageReceivedEvent(
                    $"[CommandService] SAY2 command sent successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CommandService] Error sending SAY2 command: Text='{Text}', ChatType={ChatType}", 
                    text, chatType);
                await _eventBus.PublishAsync(new LogMessageReceivedEvent(
                    $"[CommandService] Error sending SAY2 command: {ex.Message}"));
            }
        }

        /// <summary>
        /// Отправляет bypass команду к серверу
        /// </summary>
        public async Task SendBypassCommandAsync(string npcId, string action)
        {
            try
            {
                _logger.LogInformation("[CommandService] Sending bypass command: NpcId={NpcId}, Action='{Action}'", 
                    npcId, action);

                var command = new RequestBypassToServerCommand();

                string hex = PacketHexHelper.ToHex(command, Encoding.Unicode, false);
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[CommandService] Bypass command hex: {hex}"));

                await _pipeService.SendCommandAsync(hex, _processId);
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[CommandService] Bypass command sent successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CommandService] Error sending bypass command");
                await _eventBus.PublishAsync(new LogMessageReceivedEvent(
                    $"[CommandService] Error sending bypass command: {ex.Message}"));
            }
        }

        /// <summary>
        /// Отправляет команду действия с объектом
        /// </summary>
        public async Task SendActionCommandAsync(int objectId, int originX, int originY, int originZ, byte actionId = 0)
        {
            try
            {
                _logger.LogInformation("[CommandService] Sending action command: ObjectId={ObjectId}, Pos=({OriginX},{OriginY},{OriginZ}), ActionId={ActionId}", 
                    objectId, originX, originY, originZ, actionId);

                var command = new ActionCommand(objectId, originX, originY, originZ, actionId);

                string hex = PacketHexHelper.ToHex(command, Encoding.UTF8, true);
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[CommandService] Action command hex: {hex}"));

                await _pipeService.SendCommandAsync(hex, _processId);
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[CommandService] Action command sent successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CommandService] Error sending action command");
                await _eventBus.PublishAsync(new LogMessageReceivedEvent(
                    $"[CommandService] Error sending action command: {ex.Message}"));
            }
        }

        /// <summary>
        /// Отправляет команду запроса списка предметов в частных магазинах
        /// </summary>
        public async Task SendItemListCommandAsync(byte itemType = 0)
        {
            try
            {
                _logger.LogInformation("[CommandService] Sending private store item list command for ItemType: {ItemType}", itemType);

                var command = new RequestItemListCommand(itemType);

                string hex = PacketHexHelper.ToHex(command, Encoding.UTF8, true);
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[CommandService] Private store item list command hex (ItemType={itemType}): {hex}"));

                await _pipeService.SendCommandAsync(hex, _processId);
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[CommandService] Private store item list command sent successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CommandService] Error sending private store item list command");
                await _eventBus.PublishAsync(new LogMessageReceivedEvent(
                    $"[CommandService] Error sending private store item list command: {ex.Message}"));
            }
        }

        /// <summary>
        /// Отправляет команду вызова окна приватных магазинов
        /// </summary>
        public async Task SendPrivateStoreWindowCommandAsync()
        {
            try
            {
                _logger.LogInformation("[CommandService] Sending private store window command");

                var command = new RequestPrivateStoreWindowCommand();

                string hex = PacketHexHelper.ToHex(command, Encoding.UTF8, true);
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[CommandService] Private store window command hex: {hex}"));

                await _pipeService.SendCommandAsync(hex, _processId);
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[CommandService] Private store window command sent successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CommandService] Error sending private store window command");
                await _eventBus.PublishAsync(new LogMessageReceivedEvent(
                    $"[CommandService] Error sending private store window command: {ex.Message}"));
            }
        }

        /// <summary>
        /// Отправляет команду поиска предметов во всемирном магазине
        /// </summary>
        public async Task SendWorldExchangeSearchCommandAsync(byte itemType = 0x00, ushort unknown2 = 0x0002)
        {
            try
            {
                _logger.LogInformation("[CommandService] Sending world exchange search command for ItemType: 0x{ItemType:X2}, Unknown2: 0x{Unknown2:X4}", itemType, unknown2);

                var command = new ExWorldExchangeSearchItemCommand(itemType, unknown2);

                string hex = PacketHexHelper.ToHex(command, Encoding.UTF8, true);
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[CommandService] World exchange search command hex (ItemType=0x{itemType:X2}, Unknown2=0x{unknown2:X4}): {hex}"));

                await _pipeService.SendCommandAsync(hex, _processId);
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[CommandService] World exchange search command sent successfully"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[CommandService] Error sending world exchange search command");
                await _eventBus.PublishAsync(new LogMessageReceivedEvent(
                    $"[CommandService] Error sending world exchange search command: {ex.Message}"));
            }
        }


        public void Dispose()
        {
            // Никаких ресурсов для освобождения
        }
    }
}
