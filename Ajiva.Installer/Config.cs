using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ajiva.Installer.ViewModels;

namespace Ajiva.Installer
{
    internal class Config
    {
        private const string Cfg = "config.cfg";

        public static void Load()
        {
            if (File.Exists(Cfg))
            {
                Current = JsonSerializer.Deserialize<CfgLayout>(File.ReadAllText(Cfg))!;
                InstalledPrograms.Clear();
                foreach (var currentInstallerInfo in Current.InstallerInfos.ToArray())
                {
                    InstalledPrograms.Add(currentInstallerInfo);
                }
            }
            else
            {
                Current = new()
                {
                    InstallerInfos =
                    {
                        new InstalledInfo
                        {
                            Description = "This Is An Sample", Name = "Sample", Path = ".", Progress = 0,
                            IconSrc = @"C:\Windows\SystemResources\Windows.SystemToast.Calling\Images\Placeholder_buddy.png"
                        }
                    }
                };
                Save();
            }
        }

        public static void Save()
        {
            if (File.Exists(Cfg)) // do some checkin if different ?
            {
                File.Delete(Cfg);
            }

            var sjon = JsonSerializer.Serialize(Current, new() {WriteIndented = true});
            File.WriteAllText(Cfg, sjon);
        }

        public static CfgLayout Current { get; set; }

        static Config()
        {
            InstalledPrograms.CollectionChanged += delegate(object _, NotifyCollectionChangedEventArgs e)
            {
                if (Current is null) return;

                if (e.NewItems is not null)
                    foreach (var eNewItem in e.NewItems.Cast<InstalledInfo>())
                        if (Current.InstallerInfos.All(x => x.Path != eNewItem.Path && x.Name != eNewItem.Name))
                            Current.InstallerInfos.Add(eNewItem);

                if (e.OldItems is not null)
                    foreach (var eOldItem in e.OldItems.Cast<InstalledInfo>())
                        Current.InstallerInfos.RemoveAll(x => x.Path == eOldItem.Path && x.Name == eOldItem.Name);

                Save();
            };
        }

        public static ObservableCollection<InstalledInfo> InstalledPrograms { get; } = new();
    }
    internal class CfgLayout
    {
        public string DefaultPathRef = "./Installed";
        public List<InstalledInfo> InstallerInfos { get; set; } = new();

        [JsonInclude]
        public string DefaultPath
        {
            get => DefaultPathRef;
            set => DefaultPathRef = value;
        }
    }
}
