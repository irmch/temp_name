using System.Text;
using System.IO;
using L2Market.Domain.Events;
using L2Market.Domain.Common;

namespace L2Market.Domain.Entities.UserInfoPacket;

public class UserInfoPacket
{
    public uint ObjectId { get; set; }
    public uint InitBlockSize { get; set; }
    public ushort MaskBits { get; set; }
    public byte[] Mask { get; set; } = Array.Empty<byte>();
    public Dictionary<UserInfoType, object> Components { get; set; } = new();

    private static IEventBus? _eventBus;

    public static void SetEventBus(IEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    public static UserInfoPacket FromBytes(byte[] data)
    {
        var packet = new UserInfoPacket();
        
        try
        {
            using var stream = new MemoryStream(data);
            using var reader = new BinaryReader(stream);

            // Проверяем минимальный размер пакета
            if (data.Length < 1)
            {
                throw new InvalidDataException($"Packet too short: {data.Length} bytes");
            }

            // ObjectId (4 bytes)
            packet.ObjectId = reader.ReadUInt32();
            // InitBlockSize (4 bytes)
            packet.InitBlockSize = reader.ReadUInt32();
            // MaskBits (2 bytes)
            packet.MaskBits = reader.ReadUInt16();

            // Mask bytes
            var maskBytes = (packet.MaskBits + 7) / 8;
            packet.Mask = reader.ReadBytes(maskBytes);

            // Определяем включенные компоненты
            var included = new List<UserInfoType>();
            for (int i = 0; i < packet.Mask.Length; i++)
            {
                for (int bit = 7; bit >= 0; bit--)
                {
                    int maskBit = i * 8 + (7 - bit);
                    if ((packet.Mask[i] & (1 << bit)) != 0)
                    {
                        if (Enum.IsDefined(typeof(UserInfoType), (byte)maskBit))
                        {
                            included.Add((UserInfoType)maskBit);
                        }
                    }
                }
            }

            // Читаем компоненты
            foreach (var component in included)
            {
                switch (component)
                {
                    case UserInfoType.RELATION:
                        packet.Components[component] = reader.ReadUInt32();
                        break;
                    case UserInfoType.BASIC_INFO:
                        var basicInfoSize = reader.ReadUInt16();
                        var nameLength = reader.ReadUInt16();
                        var nameBytes = reader.ReadBytes(nameLength * 2);
                        string name = Encoding.Unicode.GetString(nameBytes).TrimEnd('\0');
                        var gm = reader.ReadByte();
                        var race = reader.ReadByte();
                        var female = reader.ReadByte();
                        var rootClass = reader.ReadUInt32();
                        var classId = reader.ReadUInt32();
                        var level = reader.ReadUInt32();
                        var classIdRepeat = reader.ReadUInt32();
                        
                        packet.Components[component] = new 
                        { 
                            Name = name, 
                            Gm = gm, 
                            Race = race, 
                            Female = female, 
                            RootClass = rootClass, 
                            ClassId = classId, 
                            Level = level, 
                            ClassIdRepeat = classIdRepeat 
                        };
                        break;
                    case UserInfoType.POSITION:
                        var positionSize = reader.ReadUInt16();
                        var x = reader.ReadInt32();
                        var y = reader.ReadInt32();
                        var z = reader.ReadInt32();
                        var vehicleId = reader.ReadUInt32();
                        packet.Components[component] = new { X = x, Y = y, Z = z, VehicleId = vehicleId };
                        break;
                    default:
                        // Для неизвестных компонентов читаем размер и пропускаем данные
                        if (stream.Position + 2 <= stream.Length)
                        {
                            var size = reader.ReadUInt16();
                            if (stream.Position + size <= stream.Length)
                            {
                                reader.ReadBytes(size);
                            }
                        }
                        break;
                }
            }

            return packet;
        }
        catch
        {
            throw; // Перебрасываем исключение дальше
        }
    }
}

public enum UserInfoType : byte
{
    RELATION = 0x00,
    BASIC_INFO = 0x01,
    BASE_STATS = 0x02,
    MAX_HPCPMP = 0x03,
    CURRENT_HPMPCP_EXP_SP = 0x04,
    ENCHANTLEVEL = 0x05,
    APPAREANCE = 0x06,
    STATUS = 0x07,
    STATS = 0x08,
    ELEMENTALS = 0x09,
    POSITION = 0x0A,
    SPEED = 0x0B,
    MULTIPLIER = 0x0C,
    COL_RADIUS_HEIGHT = 0x0D,
    ATK_ELEMENTAL = 0x0E,
    CLAN = 0x0F,
    SOCIAL = 0x10,
    VITA_FAME = 0x11,
    SLOTS = 0x12,
    MOVEMENTS = 0x13,
    COLOR = 0x14,
    INVENTORY_LIMIT = 0x15,
    TRUE_HERO = 0x16,
    ATT_SPIRITS = 0x17,
    RANKING = 0x18,
    STAT_POINTS = 0x19,
    STAT_ABILITIES = 0x1A,
    ELIXIR_USED = 0x1B,
    VANGUARD_MOUNT = 0x1C
}
