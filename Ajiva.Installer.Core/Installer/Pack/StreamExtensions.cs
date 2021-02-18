/*
 * I use UInt32 to force an conversion on every GetBytes to not accidentally use long or short as datatype
 */

using System;
using System.IO;
using System.Text;
using System.Text.Json;
using value_type_dec64 = System.Int64;
using value_type_dec32 = System.Int32;

namespace Ajiva.Installer.Core.Installer.Pack
{
    public static class StreamExtensions
    {
        public static void WriteString(this Stream stream, string str)
        {
            if (str.Length > ushort.MaxValue) throw new ArgumentException("String to Long!");

            stream.Write(BitConverter.GetBytes((ushort)str.Length));
            stream.Write(Encoding.UTF8.GetBytes(str));
        }

        public static string ReadString(this Stream stream)
        {
            Span<byte> tmp = new byte[sizeof(ushort)];

            stream.Read(tmp); //str length
            tmp = new byte[BitConverter.ToInt16(tmp)];
            stream.Read(tmp); //str
            return Encoding.UTF8.GetString(tmp);
        }

#region value_type_dec read / write

        public static void WriteValue64(this Stream stream, long value) => stream.Write(BitConverter.GetBytes(value));

        public static long ReadValue64(this Stream stream)
        {
            stream.Read(TmpValueSpan64);
            return BitConverter.ToInt64(TmpValueSpan64);
        }

        [ThreadStatic] private static Memory<byte>? tmpMemoryValue64;
        private static Span<byte> TmpValueSpan64
        {
            get
            {
                tmpMemoryValue64 ??= new byte[sizeof(long)];
                return tmpMemoryValue64.Value.Span;
            }
        }

        public static void WriteValue32(this Stream stream, int value) => stream.Write(BitConverter.GetBytes(value));

        public static int ReadValue32(this Stream stream)
        {
            stream.Read(TmpValueSpan32);
            return BitConverter.ToInt32(TmpValueSpan32);
        }

        [ThreadStatic] private static Memory<byte>? tmpMemoryValue32;
        private static Span<byte> TmpValueSpan32
        {
            get
            {
                tmpMemoryValue32 ??= new byte[sizeof(int)];
                return tmpMemoryValue32.Value.Span;
            }
        }

  #endregion

        public static T? ReadJsonObjectAs<T>(this Stream stream, out uint length) where T : class, new()
        {
            Span<byte> tmp = new byte[sizeof(uint)];
            stream.Read(tmp);
            var jsonLength = BitConverter.ToUInt32(tmp);
            tmp = new byte[jsonLength];
            stream.Read(tmp);
            var json = Encoding.UTF8.GetString(tmp);
            length = sizeof(uint) + jsonLength;
            return JsonSerializer.Deserialize<T>(json);
        }

        public static uint WriteAsJson<T>(this Stream stream, T value) where T : class, new()
        {
            var json = JsonSerializer.Serialize(value);
            var bits = Encoding.UTF8.GetBytes(json);
            stream.Write(BitConverter.GetBytes((uint)bits.Length));
            stream.Write(bits);
            return (uint)(bits.Length + sizeof(uint));
        }
    }
}
