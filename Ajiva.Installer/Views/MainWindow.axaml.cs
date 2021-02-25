using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Ajiva.Installer.Helpers;
using Ajiva.Installer.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Ajiva.Installer.Views
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            DataContext = new MainWindowViewModel();
            DataContextData.IsPopupVisible = false;
        }

        private MainWindowViewModel DataContextData => (DataContext as MainWindowViewModel)!;

        private void InstalledList_OnOnOptions(InstalledInfo obj)
        {
            DataContextData.IsPopupVisible = true;
            var opt = this.Get<Options>("Options");
            opt.DataContext = obj;
        }

        private void InstalledList_Ready(object? sender, EventArgs e)
        {
            (sender as InstalledList)!.OnOptions += InstalledList_OnOnOptions;
        }

        private void CloseOptions_OnClick(object? sender, RoutedEventArgs e)
        {
            DataContextData.IsPopupVisible = false;
        }

        private InstallDialog? Dialog;

        private void Add_OnClick(object? sender, RoutedEventArgs e)
        {
            if ((Dialog is not null && Dialog.IsVisible) || (Dialog == null))
                Dialog = new();
            
            Dialog.DataContext = new InstallDialogViewModel {Path = Config.Current.DefaultPathRef};
            Dialog.Show(this);
        }

        private void Save_OnClick(object? sender, RoutedEventArgs e)
        {
            Config.Save();
        }

        private void Window_OnClosing(object? sender, CancelEventArgs e)
        {
            RunHelper.Running.RemoveAll(x=> !x.Running);
            foreach (var runningInfo in RunHelper.Running)
            {
                Task.Run(() =>
                {
                    Interop.SetForegroundWindow(runningInfo.Proc.MainWindowHandle);
                    for (var i = 0; i < 2; i++)
                    {
                        Interop.FlashWindow(runningInfo.Proc.MainWindowHandle, false);
                        Task.Delay(1000);
                        Interop.FlashWindow(runningInfo.Proc.MainWindowHandle, true);
                        Task.Delay(1000);
                    }
                });
            }
            if (!RunHelper.Running.Any()) return;

            Interop.FlashWindow(PlatformImpl.Handle.Handle, false);
            e.Cancel = true;
        }
    }
}
