using System;

namespace L2Market.Domain.Entities
{

public abstract class GamePacket
{
    public string Direction { get; set; } = string.Empty; // "C" (Client) или "S" (Server)
    public int Id { get; set; } // ID пакета в hex
    public int? ExId { get; set; } // Расширенный ID (если есть)
    public int Size { get; set; } // Размер пакета
    public byte[]? Data { get; set; } // Данные пакета
    public DateTime Timestamp { get; set; } // Время получения

    public abstract void ParseFromBytes(byte[] data);
    public abstract byte[] ToBytes();

    public override string ToString()
    {
        return $"GamePacket {{ Id=0x{Id:X2}, Direction={Direction}, Size={Size}, DataLength={Data?.Length ?? 0} }}";
    }
}
}
