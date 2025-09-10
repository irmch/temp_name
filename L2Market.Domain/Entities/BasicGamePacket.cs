using System;

namespace L2Market.Domain.Entities;

/// <summary>
/// Базовая реализация GamePacket для пакетов, которые не требуют специфичного парсинга
/// </summary>
public class BasicGamePacket : GamePacket
{
    public override void ParseFromBytes(byte[] data)
    {
        // Базовая реализация - просто сохраняем данные
        Data = data;
        Size = data.Length;
    }

    public override byte[] ToBytes()
    {
        // Возвращаем сохраненные данные или пустой массив
        return Data ?? Array.Empty<byte>();
    }
}
