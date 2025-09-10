using System;

namespace L2Market.Domain.Entities
{

public class Packet
{
    public string Direction { get; set; } = ""; // "C" (Client) или "S" (Server)
    public int Id { get; set; }                 // ID пакета (0x00, 0x32, etc.)
    public int? ExId { get; set; }              // Дополнительный ID
    public int Size { get; set; }               // Размер пакета
    public byte[] Data { get; set; } = Array.Empty<byte>(); // Данные пакета
    public DateTime Timestamp { get; set; }     // Время получения
}
}
