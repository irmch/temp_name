using System.Text.Json;
using System.Text.Json.Nodes;
using L2Market.Core.Models;
using L2Market.Domain.Common;
using L2Market.Domain.Events;
using L2Market.Domain.Entities.ExPrivateStoreSearchItemPacket;
using L2Market.Domain.Entities.ExResponseCommissionListPacket;
using L2Market.Domain.Entities.WorldExchangeItemListPacket;
using Microsoft.Extensions.Logging;

namespace L2Market.Core.Services
{
    /// <summary>
    /// Service for parsing game packets from JSON data
    /// </summary>
    public class PacketParserService : IPacketParserService
    {
        private readonly IEventBus _eventBus;
        private bool _isRunning;

        public bool IsRunning => _isRunning;

        public PacketParserService(IEventBus eventBus)
        {
            _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            
            // Подписываемся на события получения данных от NamedPipe
            _eventBus.Subscribe<PipeDataReceivedEvent>(async e =>
            {
                // Убираем проверку _isRunning - обрабатываем все пакеты
                var packet = TryParsePacket(e.Data ?? string.Empty);
                if (packet != null)
                {
                    // Обрабатываем пакеты прямо здесь
                    await ProcessPacket(packet);
                }
            });
        }

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            if (_isRunning) 
            {
                await _eventBus.PublishAsync(new LogMessageReceivedEvent("[PacketParserService] Packet parser service already running"));
                return;
            }
            
            _isRunning = true;
            await _eventBus.PublishAsync(new LogMessageReceivedEvent("[TEST] PacketParserService starting - EventBus test"));
            await _eventBus.PublishAsync(new LogMessageReceivedEvent("[PacketParserService] Packet parser service started - IsRunning = true"));
            await _eventBus.PublishAsync(new LogMessageReceivedEvent("[DEBUG] PacketParserService started successfully"));
        }

        public void Stop()
        {
            if (!_isRunning) return;
            
            _isRunning = false;
            _eventBus.PublishAsync(new LogMessageReceivedEvent("[PacketParserService] Packet parser service stopped"));
        }

        // ProcessPacketDataAsync removed - now handled via subscription

        private GamePacket? TryParsePacket(string data)
        {
            try
            {
                var obj = JsonNode.Parse(data)?.AsObject();
                if (obj == null) return null;

                var direction = obj["direction"]?.ToString() ?? "";
                var id = obj["id"]?.GetValue<int>() ?? 0;
                var exid = obj["exid"]?.GetValue<int?>();
                var size = obj["size"]?.GetValue<int>() ?? 0;
                var dataHex = obj["data"]?.ToString() ?? "";
                
                // Convert hex string to byte array
                var packetData = ConvertHexStringToBytes(dataHex);

                // Create basic packet
                var packet = new BasicGamePacket
                {
                    Direction = direction,
                    Id = id,
                    ExId = exid,
                    Size = size,
                    Data = packetData,
                    Timestamp = DateTime.Now
                };
                
                return packet;
            }
            catch (Exception ex)
            {
                _eventBus.PublishAsync(new LogMessageReceivedEvent($"[PacketParserService] JSON parsing error: {ex.Message}"));
                return null;
            }
        }

        private byte[] ConvertHexStringToBytes(string hex)
        {
            if (string.IsNullOrEmpty(hex) || hex.Length % 2 != 0)
                return Array.Empty<byte>();

            try
            {
                return Enumerable.Range(0, hex.Length / 2)
                    .Select(i => Convert.ToByte(hex.Substring(i * 2, 2), 16))
                    .ToArray();
            }
            catch (Exception ex)
            {
                _eventBus.PublishAsync(new LogMessageReceivedEvent($"[PacketParserService] Error converting hex string to bytes: {ex.Message}"));
                return Array.Empty<byte>();
            }
        }

        private async Task ProcessPacket(GamePacket packet)
        {
            try
            {
                // Process packets by ID and direction
                switch (packet.Id)
                {
                    case 0x5F when packet.Direction == "S":
                        await ProcessSkillListPacket(packet);
                        break;
                    case 0x48 when packet.Direction == "S":
                        await ProcessMagicSkillUsePacket(packet);
                        break;
                    case 0x39 when packet.Direction == "C":
                        await ProcessRequestMagicSkillUsePacket(packet);
                        break;
                    case 0x56 when packet.Direction == "C":
                        await ProcessRequestActionUsePacket(packet);
                        break;
                    case 0x0C when packet.Direction == "S":
                        await ProcessNpcInfoPacket(packet);
                        break;
                    case 0x08 when packet.Direction == "S":
                        await ProcessDeleteObjectPacket(packet);
                        break;
                    case 0x00 when packet.Direction == "S":
                        await ProcessDiePacket(packet);
                        break;
                    case 0x59 when packet.Direction == "C":
                        await ProcessValidatePositionPacket(packet);
                        break;
                    case 0x18 when packet.Direction == "S":
                        // StatusUpdate packet - log occasionally to reduce spam
                        if (DateTime.Now.Second % 10 == 0) // Log every 10 seconds
                        {
                            await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[PacketParserService] StatusUpdate packet (0x18) - Size: {packet.Size}"));
                        }
                        break;
                    case 0x32 when packet.Direction == "S":
                        await ProcessCharInfoPacket(packet);
                        break;
                    case 0xFE when packet.ExId == 0x02D4 && packet.Direction == "S":
                        await ProcessExPrivateStoreSearchItemPacket(packet);
                        break;
                    case 0xFE when packet.ExId == 0x02FD && packet.Direction == "S":
                        await ProcessWorldExchangeItemListPacket(packet);
                        break;
                    case 0xFE when packet.ExId == 0x00F8 && packet.Direction == "S":
                        await ProcessExResponseCommissionListPacket(packet);
                        break;
                    default:
                        // Unknown packet - process silently
                        break;
                }
            }
            catch (Exception ex)
            {
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[PacketParserService] Error processing packet {packet.FullId}: {ex.Message}"));
            }
        }

        #region Packet Processing Methods

        private async Task ProcessSkillListPacket(GamePacket packet)
        {
            await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[PacketParserService] Received skill list packet (0x5F)"));
        }

        private async Task ProcessMagicSkillUsePacket(GamePacket packet)
        {
            await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[PacketParserService] Received magic skill use packet (0x48)"));
        }

        private async Task ProcessRequestMagicSkillUsePacket(GamePacket packet)
        {
            await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[PacketParserService] Received request magic skill use packet (0x39)"));
        }

        private async Task ProcessRequestActionUsePacket(GamePacket packet)
        {
            await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[PacketParserService] Received request action use packet (0x56)"));
        }

        private async Task ProcessNpcInfoPacket(GamePacket packet)
        {
            await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[PacketParserService] Received NPC info packet (0x0C)"));
        }

        private async Task ProcessDeleteObjectPacket(GamePacket packet)
        {
            await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[PacketParserService] Received delete object packet (0x08)"));
        }

        private async Task ProcessDiePacket(GamePacket packet)
        {
            await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[PacketParserService] Received die packet (0x00)"));
        }

        private async Task ProcessValidatePositionPacket(GamePacket packet)
        {
            await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[PacketParserService] Received validate position packet (0x59)"));
        }

        private Task ProcessCharInfoPacket(GamePacket packet)
        {
            // Character info packet processed silently
            return Task.CompletedTask;
        }

        private async Task ProcessExPrivateStoreSearchItemPacket(GamePacket packet)
        {
            try
            {
                var storePacket = ExPrivateStoreSearchItemPacket.FromBytes(packet.Data);
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[PrivateStore] Page {storePacket.Page}/{storePacket.MaxPage}, Items: {storePacket.Items.Count}"));
                
                // Публикуем событие для PrivateStoreService
                await _eventBus.PublishAsync(new PrivateStoreUpdatedEvent(storePacket.Items));
            }
            catch (Exception ex)
            {
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[PacketParserService] Error processing ExPrivateStoreSearchItem packet: {ex.Message}"));
            }
        }

        private async Task ProcessWorldExchangeItemListPacket(GamePacket packet)
        {
            try
            {
                var worldExchangePacket = WorldExchangeItemListPacket.FromBytes(packet.Data);
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[WorldExchange] Category {worldExchangePacket.Category}, Page {worldExchangePacket.Page}, Items: {worldExchangePacket.Items.Count}"));
                
                // Публикуем событие для WorldExchangeService
                await _eventBus.PublishAsync(new WorldExchangeUpdatedEvent(worldExchangePacket.Items, worldExchangePacket.Category));
            }
            catch (Exception ex)
            {
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[PacketParserService] Error processing WorldExchangeItemList packet: {ex.Message}"));
            }
        }

        private async Task ProcessExResponseCommissionListPacket(GamePacket packet)
        {
            try
            {
                var commissionPacket = ExResponseCommissionListPacket.FromBytes(packet.Data);
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[Commission] ReplyType {commissionPacket.ReplyType}, Chunk {commissionPacket.ChunkId}, Items: {commissionPacket.Items.Count}"));
                
                // Публикуем событие для CommissionService
                await _eventBus.PublishAsync(new CommissionUpdatedEvent(commissionPacket.Items));
            }
            catch (Exception ex)
            {
                await _eventBus.PublishAsync(new LogMessageReceivedEvent($"[PacketParserService] Error processing ExResponseCommissionList packet: {ex.Message}"));
            }
        }

        #endregion
    }
}
