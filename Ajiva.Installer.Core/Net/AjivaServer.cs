using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Ajiva.Installer.Core.Net
{
    public class AjivaServer
    {
        private readonly TcpListener listener;

        public AjivaServer(IPEndPoint iEndpoint)
        {
            listener = new(iEndpoint);
        }

        public async Task Start(CancellationToken cancellationToken, Action<string> logger)
        {
            listener.Start();
            logger("Started Server!");
            while (!cancellationToken.IsCancellationRequested)
            {
                if (!listener.Pending())
                {
                    await Task.Delay(100, cancellationToken);

                    continue;
                }

                logger("Got Client!");
                var client = await listener.AcceptTcpClientAsync();
                await Task.Factory.StartNew(async () => await HandleClient(client, cancellationToken), cancellationToken);
            }
        }

        private static async Task HandleClient(TcpClient client, CancellationToken cancellationToken)
        {
            if (await AjivaNetUtils.HandShake(client, true, cancellationToken))
            {
                await AjivaNetUtils.SendPaket(client, new() {Type = PaketType.Hello, Version = 1}, new(0), cancellationToken);
                await AjivaNetUtils.ClientLoop(client, ServerClientCallback, cancellationToken);
            }
        }

        private static async void ServerClientCallback(AjivaNetHead arg1, AjivaMemory arg2, TcpClient arg3, CancellationToken arg4)
        {
            Console.WriteLine($"Packet Server got {arg2.Length} v:{arg1.Version}, {arg2.AsString()}");

            await AjivaNetUtils.SendPaket(arg3, new() {Type = PaketType.Response, Version = 1}, AjivaMemory.String("Hello back from server"), arg4);
        }
    }
}
