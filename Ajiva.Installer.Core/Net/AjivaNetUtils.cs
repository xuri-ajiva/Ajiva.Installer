using System;
using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Ajiva.Installer.Core.Net
{
    internal static class AjivaNetUtils
    {
        public static int MaxWaitTime { get; set; } = 30000;

        public static async Task ClientLoop(TcpClient client, Action<AjivaNetHead, AjivaMemory, TcpClient, CancellationToken> callback, CancellationToken cancellationToken)
        {
            var header = NewHeader();

            while (client.Connected && !cancellationToken.IsCancellationRequested)
            {
                var (head, data) = await GetPaket(client, cancellationToken, header);

                callback(head, data, client, cancellationToken);
            }
            if (client.Connected)
                client.Close();
        }

        public static async Task<(AjivaNetHead head, AjivaMemory data)> GetPaket(TcpClient client, CancellationToken cancellationToken, AjivaMemory? header = null)
        {
            header ??= NewHeader();
            var str = client.GetStream();

            while (client.Available < header.Length)
            {
                await Task.Delay(1, cancellationToken);
            }

            var read = str.Read(header.Span);
            Debug.Assert(read == header.Length);
            var head = Decode<AjivaNetHead>(header);

            if (head.DataLength == 0) return (head, new(0));

            var data = new AjivaMemory(head.DataLength);
            for (var i = 0; i < MaxWaitTime && client.Available < data.Length; i++)
            {
                await Task.Delay(1, cancellationToken);
            }

            read = str.Read(data.Span);
            Debug.Assert(read == data.Length);
            return (head, data);
        }

        private static int HeaderSize { get; } = Unsafe.SizeOf<AjivaNetHead>();

        public static AjivaMemory NewHeader()
        {
            return new(HeaderSize);
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

        public static async Task<bool> HandShake(TcpClient client, bool server, CancellationToken cancellationToken)
        {
            var support = new AjivaNetHead {Type = PaketType.HandShake, Version = 1, Index = 0};

            bool VersionMisMatch(AjivaNetHead remote)
            {
                Console.WriteLine($"Versions: support:{support.Version}, remote:{remote.Version}");
                return support.Version != remote.Version;
            }

            var trust = Guid.NewGuid();

            if (server) await SendPaket(client, support, AjivaMemory.With(trust), cancellationToken);

            var (head, data) = await GetPaket(client, cancellationToken);

            var href = 0;
            Guid check;
            if (server)
            {
                if (VersionMisMatch(head)) return false;

                check = data.Read<Guid>(ref href);
                Console.WriteLine($"Handshake Part Server: {trust}, Client: {check}");
                return check == trust;
            }
            if (VersionMisMatch(head)) return false;

            check = data.Read<Guid>(ref href);
            Console.WriteLine($"Handshake Part Server: {check}");
            await SendPaket(client, support, AjivaMemory.With(check), cancellationToken);
            return true;
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
