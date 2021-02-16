using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Ajiva.Installer.Core.Net
{
    public class AjivaServer<T> where T : Enum
    {
        private readonly T handShake;
        private readonly TcpListener listener;

        public Queue<ClientBody> ReceivedPackets = new();
        public PacketReceivedDelegate? PacketReceived;

        public class ClientBody
        {
            public T Type { get; set; }
            public AjivaMemory Memory { get; set; }
            public AjivaClient<T> Client { get; set; }

            public ClientBody(T type, AjivaMemory memory, AjivaClient<T> client)
            {
                Type = type;
                Memory = memory;
                Client = client;
            }

            /// <inheritdoc />
            public override string ToString()
            {
                return $"{nameof(Type)}: {Type}, {nameof(Client)}: {Client.ClientId}, {nameof(Memory)}: {Memory.AsString()}";
            }
        }

        /// <returns>if packet should be added to ReceivedPackets</returns>
        public delegate bool PacketReceivedDelegate(ClientBody body);

        public event Action<AjivaClient<T>>? OnClientHandShakeSucceeded;

        public AjivaServer(IPEndPoint iEndpoint, T handShake)
        {
            this.handShake = handShake;
            listener = new(iEndpoint);
        }

        public int currentId { get; private set; } = 1;

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
                var client = AjivaClient<T>.ServerClient(await listener.AcceptTcpClientAsync(), handShake, currentId++);

                await SetupClient(client, cancellationToken);
            }
        }

        private async Task SetupClient(AjivaClient<T> client, CancellationToken cancellationToken)
        {
            client.OnHandShakeFailed += () =>
            {
                //todo
            };
            client.OnHandShakeSucceeded += () =>
            {
                clients.Add(client);
                OnClientHandShakeSucceeded?.Invoke(client);
                Task.Factory.StartNew(async () =>
                {
                    await client.RunClient(cancellationToken);
                }, cancellationToken);
            };
            client.PacketReceived = body => ClientPacketReceived(client, body);

            await client.HandShake(true, cancellationToken);
        }

        private bool ClientPacketReceived(AjivaClient<T> ajivaClient, Body<T> body)
        {
            var clientBody = new ClientBody(body.Type, body.Memory, ajivaClient);

            if (PacketReceived == null || PacketReceived.Invoke(clientBody)) ReceivedPackets.Enqueue(clientBody);

            return false;
        }

        private List<AjivaClient<T>> clients = new();
    }
}
