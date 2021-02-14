using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Ajiva.Installer.Core.ConsoleExt;
using Ajiva.Installer.Core.Installer;
using Ajiva.Installer.Core.Net;

namespace Ajiva.Installer.Core
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            CancellationTokenSource source = new();

            var installer = new AjivaInstaller(16, new ConsoleRolBlock(10).WriteNext);
            installer.PersentageChanged += d => new ConsoleBlock(1).WriteAt("Installer: " + d, 0);

            AjivaInstallPacker packer = new();

            void CopyExample()
            {
                var installInfo = AjivaInstallInfo.DirCopy(LogHelper.GetInput("Path"));

                installer.InstallAsync(installInfo, "../cpy");
                installInfo = null;
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
            AjivaServer server = new(new(IPAddress.Any, port));
            AjivaClient client = new(new(IPAddress.Parse("127.0.0.1"), port));

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
    }
}
