using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Ajiva.Installer.Core.ConsoleExt;
using Ajiva.Installer.Core.Installer;
using Ajiva.Installer.Core.Installer.Pack;
using Ajiva.Installer.Core.Net;

namespace Ajiva.Installer.Core
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            CancellationTokenSource source = new();

            var b1In = new ConsoleBlock(1);
            Action<string> lochPtr = new ConsoleRolBlock(20).WriteNext;
            var installer = new AjivaInstaller(16, lochPtr);
            installer.PercentageChanged += d => b1In.WriteAt("Installer: " + d, 0);

            AjivaInstallPacker packer = new AjivaInstallPacker(lochPtr);

            void CopyExample()
            {
                //var installInfo = AjivaInstallInfo.DirCopy(LogHelper.GetInput("Path"));

                //installer.InstallAsync(installInfo, "../cpy");
                //installInfo = null;
            }

            void PackSome()
            {
                packer.Pack(LogHelper.GetInput("Path"), LogHelper.GetInput("Name"));
            }

            void FromSomePack()
            {
                var installInfo = packer.FromPack(LogHelper.GetInput("Path"));

                installer.InstallAsync(installInfo, "../cpy");
                installInfo = null;
            }

            ConsoleMenu menu = new();
            menu.ShowMenu("Select Action: ", new ConsoleMenuItem("Copy: ", CopyExample), new ConsoleMenuItem("Pack: ", PackSome), new ConsoleMenuItem("FromPack: ", FromSomePack));

            //ServerTest(source);

            Console.ReadKey();
            source.Cancel();
            Thread.Sleep(1000);
        }

        private static void ServerTest(CancellationTokenSource source)
        {
            const int port = 8347;

            void StartOneServer()
            {
                AjivaServer<PaketType> server = new(new(IPAddress.Any, port), PaketType.HandShake);

                server.OnClientHandShakeSucceeded += ajivaClient =>
                {
                    ajivaClient.SendPackets.Enqueue(new(PaketType.Hello, AjivaMemory.Empty));
                };

                server.PacketReceived = body =>
                {
                    Console.WriteLine(body);

                    body.Client.SendPackets.Enqueue(new(PaketType.Response, AjivaMemory.String("Hello from server")));
                    return false;
                };
                Task.Run(async () =>
                {
                    try
                    {
                        await server.Start(source.Token, Console.WriteLine);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }, source.Token);
            }

            void StartOneClient()
            {
                AjivaClient<PaketType> client = AjivaClient<PaketType>.Client(new(IPAddress.Parse("127.0.0.1"), port), PaketType.HandShake);
                client.PacketReceived = body =>
                {
                    Console.WriteLine(body);
                    client.SendPackets.Enqueue(new(PaketType.Response, AjivaMemory.String($"Hello from client {client.ClientId}")));

                    return false;
                };

                Thread.Sleep(100);
                Task.Run(async () =>
                {
                    try
                    {
                        await client.Connect(source.Token);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }, source.Token);
            }

            StartOneServer();

            StartOneClient();
            StartOneClient();
            StartOneClient();
            StartOneClient();
        }
    }
}
