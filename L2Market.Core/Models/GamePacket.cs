namespace L2Market.Core.Models
{
    /// <summary>
    /// Base class for game packets
    /// </summary>
    public abstract class GamePacket
    {
        /// <summary>
        /// Packet direction (C = Client to Server, S = Server to Client)
        /// </summary>
        public string Direction { get; set; } = string.Empty;

        /// <summary>
        /// Packet ID
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Extended packet ID (for extended packets)
        /// </summary>
        public int? ExId { get; set; }

        /// <summary>
        /// Packet size in bytes
        /// </summary>
        public int Size { get; set; }

        /// <summary>
        /// Raw packet data
        /// </summary>
        public byte[] Data { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// Timestamp when packet was received
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.Now;

        /// <summary>
        /// Gets the full packet identifier (ID or ID:ExID for extended packets)
        /// </summary>
        public string FullId => ExId.HasValue ? $"{Id:X2}:{ExId.Value:X4}" : $"{Id:X2}";

        /// <summary>
        /// Gets a human-readable description of the packet
        /// </summary>
        public abstract string GetDescription();
    }

    /// <summary>
    /// Basic implementation of GamePacket
    /// </summary>
    public class BasicGamePacket : GamePacket
    {
        public override string GetDescription()
        {
            return $"Packet {FullId} ({Direction}) - {Size} bytes";
        }
    }
}
