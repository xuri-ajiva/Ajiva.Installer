using ReactiveUI;

namespace Ajiva.Installer.ViewModels
{
    public class InstallDialogViewModel : ViewModelBase
    {
        private bool isPopupVisible = false;
        private string url;
        private string key;
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
    }
}