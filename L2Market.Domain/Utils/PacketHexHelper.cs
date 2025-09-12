using System;
using System.IO;
using System.Reflection;
using System.Numerics;
using System.Text;

namespace L2Market.Domain.Utils
{
    /// <summary>
    /// Утилита для конвертации объектов пакетов в hex строки
    /// </summary>
    public static class PacketHexHelper
    {

        /// <summary>
        /// Конвертирует объект в hex строку с поддержкой строк без размера
        /// </summary>
        /// <typeparam name="T">Тип объекта</typeparam>
        /// <param name="obj">Объект для конвертации</param>
        /// <param name="stringEncoding">Кодировка для строк (по умолчанию UTF-8)</param>
        /// <param name="includeStringLength">Включать ли размер строки в начало (по умолчанию true)</param>
        /// <returns>Hex строка</returns>
        public static string ToHex<T>(T obj, Encoding? stringEncoding = null, bool includeStringLength = true)
        {
            stringEncoding ??= Encoding.UTF8;
            using var ms = new MemoryStream();
            using var bw = new BinaryWriter(ms);

            foreach (var field in typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                var value = field.GetValue(obj);
                var type = field.FieldType;

                if (type == typeof(byte))
                    bw.Write((byte)value!);
                else if (type == typeof(short))
                    bw.Write((short)value!);
                else if (type == typeof(ushort))
                    bw.Write((ushort)value!);
                else if (type == typeof(int))
                    bw.Write((int)value!);
                else if (type == typeof(uint))
                    bw.Write((uint)value!);
                else if (type == typeof(long))
                    bw.Write((long)value!);
                else if (type == typeof(ulong))
                    bw.Write((ulong)value!);
                else if (type == typeof(float))
                    bw.Write((float)value!);
                else if (type == typeof(double))
                    bw.Write((double)value!);
                else if (type == typeof(bool))
                    bw.Write((bool)value! ? (byte)1 : (byte)0);
                else if (type == typeof(string))
                {
                    var str = (string?)value ?? "";
                    var bytes = stringEncoding.GetBytes(str);
                    
                    if (includeStringLength)
                    {
                        bw.Write((ushort)bytes.Length); // длина строки (2 байта)
                    }
                    bw.Write(bytes);
                    
                    // Для UTF-16LE добавляем нулевой терминатор
                    if (stringEncoding == Encoding.Unicode)
                    {
                        bw.Write((ushort)0); // нулевой терминатор для UTF-16LE
                    }
                }
                else if (type == typeof(Vector3))
                {
                    var v = (Vector3)value!;
                    bw.Write(v.X);
                    bw.Write(v.Y);
                    bw.Write(v.Z);
                }
                else
                    throw new NotSupportedException($"Field {field.Name} of type {type.Name} is not supported");
            }
            
            // Обрабатываем свойства
            foreach (var property in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (property.CanRead && property.GetIndexParameters().Length == 0) // Только свойства без параметров
                {
                    var value = property.GetValue(obj);
                    var type = property.PropertyType;

                    if (type == typeof(byte))
                        bw.Write((byte)value!);
                    else if (type == typeof(short))
                        bw.Write((short)value!);
                    else if (type == typeof(ushort))
                        bw.Write((ushort)value!);
                    else if (type == typeof(int))
                        bw.Write((int)value!);
                    else if (type == typeof(uint))
                        bw.Write((uint)value!);
                    else if (type == typeof(long))
                        bw.Write((long)value!);
                    else if (type == typeof(ulong))
                        bw.Write((ulong)value!);
                    else if (type == typeof(float))
                        bw.Write((float)value!);
                    else if (type == typeof(double))
                        bw.Write((double)value!);
                    else if (type == typeof(bool))
                        bw.Write((bool)value! ? (byte)1 : (byte)0);
                    else if (type == typeof(string))
                    {
                        var str = (string?)value ?? "";
                        var bytes = stringEncoding.GetBytes(str);
                        
                        if (includeStringLength)
                        {
                            bw.Write((ushort)bytes.Length); // длина строки (2 байта)
                        }
                        bw.Write(bytes);
                        
                        // Для UTF-16LE добавляем нулевой терминатор
                        if (stringEncoding == Encoding.Unicode)
                        {
                            bw.Write((ushort)0); // нулевой терминатор для UTF-16LE
                        }
                    }
                    else if (type == typeof(Vector3))
                    {
                        var v = (Vector3)value!;
                        bw.Write(v.X);
                        bw.Write(v.Y);
                        bw.Write(v.Z);
                    }
                    else
                        throw new NotSupportedException($"Property {property.Name} of type {type.Name} is not supported");
                }
            }

            return BitConverter.ToString(ms.ToArray()).Replace("-", "");
        }

        /// <summary>
        /// Конвертирует объект в hex строку с UTF-16LE кодировкой
        /// </summary>
        /// <typeparam name="T">Тип объекта</typeparam>
        /// <param name="obj">Объект для конвертации</param>
        /// <param name="includeStringLength">Включать ли размер строки в начало (по умолчанию true)</param>
        /// <returns>Hex строка</returns>
        public static string ToHexUtf16Le<T>(T obj, bool includeStringLength = true)
        {
            return ToHex(obj, Encoding.Unicode, includeStringLength);
        }

        /// <summary>
        /// Конвертирует объект в hex строку с UTF-8 кодировкой
        /// </summary>
        /// <typeparam name="T">Тип объекта</typeparam>
        /// <param name="obj">Объект для конвертации</param>
        /// <param name="includeStringLength">Включать ли размер строки в начало (по умолчанию true)</param>
        /// <returns>Hex строка</returns>
        public static string ToHexUtf8<T>(T obj, bool includeStringLength = true)
        {
            return ToHex(obj, Encoding.UTF8, includeStringLength);
        }

        /// <summary>
        /// Конвертирует объект в hex строку с ASCII кодировкой
        /// </summary>
        /// <typeparam name="T">Тип объекта</typeparam>
        /// <param name="obj">Объект для конвертации</param>
        /// <param name="includeStringLength">Включать ли размер строки в начало (по умолчанию true)</param>
        /// <returns>Hex строка</returns>
        public static string ToHexAscii<T>(T obj, bool includeStringLength = true)
        {
            return ToHex(obj, Encoding.ASCII, includeStringLength);
        }
    }
}
