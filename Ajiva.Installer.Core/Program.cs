using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Ajiva.Installer.Core.ConsoleExt;
using Ajiva.Installer.Core.Installer;
using Ajiva.Installer.Core.Installer.Pack;
using Ajiva.Installer.Core.Net;

namespace Ajiva.Installer.Core
{
    public static class Program
    {
        private static readonly Action<string> LockPtr = new ConsoleRolBlock(20).WriteNext;
        private static readonly ConsoleBlock B1Ln = new(1);
        private static readonly Action<string> B1In = s => B1Ln.WriteAt(s, 0);

        public static readonly AjivaInstaller Installer = new(16, LockPtr);

        private static StreamWriter log;

        private static void OpenLogFile()
        {
            Directory.CreateDirectory("Logs");
            const string logExt = ".log";
            var logName = $"Logs/log-{DateTime.Now:yyyy-mm-dd-hh-ss}";
            while (File.Exists(logName + logExt)) logName += "(2)";
            log = new(File.Open(logName + logExt, FileMode.OpenOrCreate, FileAccess.Write));
        }

        private static readonly AjivaInstallPacker Packer = new(str =>
        {
            LockPtr(str);
            log.WriteLine(str);
        });

        public static AjivaInstallInfo Install(RunInfo info, bool waitForFinish, Action<double> percentageChanged)
        {
            var installInfo = Packer.FromPack(info.PackPath);

            Installer.InstallAsync(installInfo, info.InstallPath, percentageChanged);

            if (!waitForFinish) return installInfo;

            while (!Installer.IsFinished)
            {
                Thread.Sleep(10);
            }
            return installInfo;
        }

        public static void Main(string[] args)
        {
            OpenLogFile();
            if (args.Length > 0)
            {
                try
                {
                    var info = JsonSerializer.Deserialize<RunInfo>(args[0])!;

                    Install(info, true, d => B1In("Installer: " + d));

                    Environment.Exit(0);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    throw;
                }
            }

            CancellationTokenSource source = new();

            void PackSome()
            {
                Packer.BuildPack(LogHelper.GetInput("Path"), new(LogHelper.GetInput("Name"), LogHelper.GetInput("Description"),LogHelper.GetInput("IconSrc") , LogHelper.GetInput("Executable"), LogHelper.GetInput("Arguments")));
            }

            void FromSomePack()
            {
                var installInfo = Packer.FromPack(LogHelper.GetInput("Path"));

                Installer.InstallAsync(installInfo, "../cpy", d => B1In("Installer: " + d));
                installInfo = null;
            }

            ConsoleMenu menu = new();
            menu.ShowMenu("Select Action: ", new ConsoleMenuItem("Pack: ", PackSome), new ConsoleMenuItem("FromPack: ", FromSomePack));

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
    public class RunInfo
    {
        public string PackPath { get; set; }
        public string InstallPath { get; set; }
    }
}
