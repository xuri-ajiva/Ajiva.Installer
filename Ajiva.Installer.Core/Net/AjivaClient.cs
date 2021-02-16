using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Ajiva.Installer.Core.Net
{
    public class AjivaClient
    {
        private readonly IPEndPoint iEndpoint;
        private readonly TcpClient client;

        public AjivaClient(IPEndPoint iEndpoint)
        {
            this.iEndpoint = iEndpoint;
            client = new();
        }

        public async Task Connect(CancellationToken cancellationToken)
        {
            client.Connect(iEndpoint);

            await Task.Factory.StartNew(async () => await HandleClient(client, cancellationToken), cancellationToken);
        }

        private static async Task HandleClient(TcpClient client, CancellationToken cancellationToken)
        {
            if (await AjivaNetUtils.HandShake(client, false, cancellationToken))
            {
                await AjivaNetUtils.ClientLoop(client, ClientCallback, cancellationToken);
            }
        }

        private static async void ClientCallback(AjivaNetHead arg1, AjivaMemory arg2, TcpClient arg3, CancellationToken arg4)
        {
            Console.WriteLine($"Packet Client got {arg2.Length} v:{arg1.Version}, {arg2.AsString()}");

            await AjivaNetUtils.SendPaket(arg3, new() {Type = PaketType.Response, Version = 1}, AjivaMemory.String("Hello Server"), arg4);
        }
    }
}
