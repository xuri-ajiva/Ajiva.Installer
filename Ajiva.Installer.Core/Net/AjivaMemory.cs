using System;
using System.Buffers;
using System.ComponentModel.Design;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Ajiva.Installer.Core.Net
{
    public class AjivaMemory
    {
        public int Length => mem.Length;
        private readonly Memory<byte> mem;
        public Span<byte> Span => mem.Span;
        public ReadOnlyMemory<byte> Memory => mem;

        private byte[]? array;

        public AjivaMemory(int size)
        {
            array = ArrayPool<byte>.Shared.Rent(size);
            mem = array.AsMemory(0, size);
        }

        public AjivaMemory(Memory<byte> bytes)
        {
            array = null;
            mem = bytes;
        }

        ~AjivaMemory()
        {
            if (array is not null)
                ArrayPool<byte>.Shared.Return(array, true);
        }

        public Span<byte> RefSlice(ref int head, int size)
        {
            var sp = mem.Span.Slice(head, size);
            head += size;
            return sp;
        }

        public void Write<T>(ref int head, T value) where T : struct
        {
            var pin = mem.Slice(head).Pin();
            unsafe
            {
                Marshal.StructureToPtr(value, new(pin.Pointer), true);
            }
            head += Unsafe.SizeOf<T>();
        }

        public void Write(ref int head, byte[] value)
        {
            var pin = mem.Slice(head).Span;

            var start = head;

            for (; head < start + value.Length; head++)
                pin[head] = value[head];
        }

        public T Read<T>(ref int head) where T : struct
        {
            T res;
            var pin = mem.Slice(head).Pin();
            unsafe
            {
                res = Marshal.PtrToStructure<T>(new(pin.Pointer));
            }
            head += Unsafe.SizeOf<T>();
            return res;
        }

        public static AjivaMemory Empty => new AjivaMemory(0);

        public static AjivaMemory String(string message)
        {
            var ret = new AjivaMemory(message.Length);

            Encoding.UTF8.GetBytes(message, ret.Span);

            return ret;
        }

        public string AsString()
        {
            return Encoding.UTF8.GetString(mem.Span);
        }

        public static AjivaMemory With<T>(T value) where T : struct
        {
            var size = Unsafe.SizeOf<T>();
            var ret = new AjivaMemory(size);
            var head = 0;
            ret.Write(ref head, value);
            return ret;
        }
    }
}
