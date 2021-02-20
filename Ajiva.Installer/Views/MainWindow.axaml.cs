using System;
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
            Dialog ??= new();
            Dialog.DataContext = new InstallDialogViewModel() {Path = Config.Current.DefaultPathRef};

            Dialog.Show(this);
        }

        private void Save_OnClick(object? sender, RoutedEventArgs e)
        {
            Config.Save();
        }
    }
}
