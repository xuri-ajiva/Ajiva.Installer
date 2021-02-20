using ReactiveUI;

namespace Ajiva.Installer.ViewModels
{
    public class InstallDialogViewModel : ViewModelBase
    {
        private bool isPopupVisible = false;
        private string url;
        private string key;
        private string path;
        public bool IsPopupVisible
        {
            get => isPopupVisible;
            set => this.RaiseAndSetIfChanged(ref isPopupVisible, value);
        }
        public string Url
        {
            get => url;
            set => this.RaiseAndSetIfChanged(ref url, value);
        }
        public string Key
        {
            get => key;
            set => this.RaiseAndSetIfChanged(ref key, value);
        }
        public string Path
        {
            get => path;
            set => this.RaiseAndSetIfChanged(ref path, value);
        }
    }
}
