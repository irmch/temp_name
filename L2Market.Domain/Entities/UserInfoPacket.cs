using System.Text;
using System.IO;
using System;

namespace L2Market.Domain.Entities
{
public class UserInfoPacket : GamePacket
{
    public uint ObjectId { get; set; }
    public string Name { get; set; } = "";
    public int Level { get; set; }
    public int ClassId { get; set; }
    public Vector3 Position { get; set; }
    
    public UserInfoPacket()
    {
        Id = 0x32;
        Direction = "S";
    }
    
    public override void ParseFromBytes(byte[] data)
    {
        using var stream = new MemoryStream(data);
        using var reader = new BinaryReader(stream);
        
        // Проверяем ID пакета
        byte packetId = reader.ReadByte();
        if (packetId != Id)
            throw new InvalidDataException($"Unexpected packet ID: 0x{packetId:X2}");
        
        // Парсим данные
        ObjectId = reader.ReadUInt32();
        
        // Читаем имя (Unicode, 2 байта на символ)
        var nameLength = reader.ReadUInt16();
        var nameBytes = reader.ReadBytes(nameLength * 2);
        Name = Encoding.Unicode.GetString(nameBytes).TrimEnd('\0');
        
        // Читаем остальные поля
        Level = reader.ReadInt32();
        ClassId = reader.ReadInt32();
        
        // Позиция (3 float значения)
        Position = new Vector3(
            reader.ReadSingle(),
            reader.ReadSingle(),
            reader.ReadSingle()
        );
    }
    
    public override byte[] ToBytes()
    {
        using var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream);
        
        writer.Write((byte)Id);
        writer.Write(ObjectId);
        
        var nameBytes = Encoding.Unicode.GetBytes(Name);
        writer.Write((ushort)(nameBytes.Length / 2));
        writer.Write(nameBytes);
        
        writer.Write(Level);
        writer.Write(ClassId);
        writer.Write(Position.X);
        writer.Write(Position.Y);
        writer.Write(Position.Z);
        
        return stream.ToArray();
    }
}

public struct Vector3
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Z { get; set; }
    
    public Vector3(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}
}
