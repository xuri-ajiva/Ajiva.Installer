using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using ReactiveUI;

namespace Ajiva.Installer.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private bool isPopupVisible = false;

        public ObservableCollection<InstalledInfo> Items { get; } = new(new[]
        {
            new InstalledInfo
            {
                Description = "Walk the dog", Name = "St", IconSrc = @"./avalonia-logo.ico", ExecutingOptions =
                {
                    Args = "/c echo Description && timeout 10",
                    Executable = "cmd.exe"
                }
            },
            new InstalledInfo {Description = "Buy some milk", Name = "Main"},
            new InstalledInfo {Description = "Learn Avalonia", Name = "Factorio"},
        });

        public bool IsPopupVisible
        {
            get => isPopupVisible;
            set => this.RaiseAndSetIfChanged(ref isPopupVisible, value);
        }
    }
}
