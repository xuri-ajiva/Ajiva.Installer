using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Ajiva.Installer.Core.Net
{
    delegate void DataReceivedCallback<T>(AjivaNetHead head, AjivaMemory body, TcpClient client, CancellationToken cancellationToken) where T : Enum;

    internal static class AjivaNetUtils
    {
        public static int MaxWaitTime { get; set; } = 30000;
        
        public static async Task<(AjivaNetHead head, AjivaMemory data)> GetPaket<T>(TcpClient client, CancellationToken cancellationToken, AjivaMemory? header = null) where T : Enum
        {
            header ??= NewHeader();
            var str = client.GetStream();

            while (client.Available < header.Length) await Task.Delay(1, cancellationToken);

            var read = str.Read(header.Span);
            Debug.Assert(read == header.Length);
            var head = Decode<AjivaNetHead>(header);

            if (head.DataLength == 0) return (head, new(0));

            var data = new AjivaMemory(head.DataLength);

            for (var i = 0; i < MaxWaitTime && client.Available < data.Length; i++) await Task.Delay(1, cancellationToken);

            read = str.Read(data.Span);
            Debug.Assert(read == data.Length);
            return (head, data);
        }

        public static AjivaMemory NewHeader()
        {
            return new(Unsafe.SizeOf<AjivaNetHead>());
        }

        public static T Decode<T>(AjivaMemory memory) where T : struct
        {
            var head = 0;
            return memory.Read<T>(ref head);
        }

        public static AjivaMemory Encode<T>(T value) where T : struct
        {
            var head = 0;
            var ret = NewHeader();
            ret.Write(ref head, value);
            return ret;
        }



        public static async Task SendPaket(TcpClient client, AjivaNetHead head, AjivaMemory data, CancellationToken cancellationToken)
        {
            head.DataLength = data.Length;
            var encHead = Encode(head);
            var str = client.GetStream();
            await str.WriteAsync(encHead.Memory, cancellationToken);
            await str.WriteAsync(data.Memory, cancellationToken);
            await str.FlushAsync(cancellationToken);
        }
    }
}
