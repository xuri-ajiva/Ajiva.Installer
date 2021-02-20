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

        public ObservableCollection<InstalledInfo> Items => Config.InstalledPrograms;
        public bool IsPopupVisible
        {
            get => isPopupVisible;
            set => this.RaiseAndSetIfChanged(ref isPopupVisible, value);
        }
        public string DefaultInstallPath
        {
            get => Config.Current.DefaultPathRef;
            set => this.RaiseAndSetIfChanged(ref Config.Current.DefaultPathRef, value);
        }
    }
}
