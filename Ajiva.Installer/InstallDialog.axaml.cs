using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Ajiva.Installer.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using DynamicData;

namespace Ajiva.Installer
{
    public class InstallDialog : Window
    {
        public InstallDialog()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private InstallDialogViewModel DataContextData => (DataContext as InstallDialogViewModel)!;

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void CloseOptions_OnClick(object? sender, RoutedEventArgs e)
        {
            DataContextData.IsPopupVisible = false;
        }

        private TabControl Pages => this.Get<TabControl>("Pages");

        private void Next_OnClick(object? sender, RoutedEventArgs e)
        {
            Pages.SelectedIndex = (Pages.SelectedIndex + 1) % Pages.Items.Cast<object>().Count();
        }

        public bool Installed;

        private void Finish_OnClick(object? sender, RoutedEventArgs e)
        {
            if (Installed)
            {
                Close(true);
                return;
            }

            if (string.IsNullOrEmpty(DataContextData.Path))
            {
                Back_OnClick(null, null!);
                return;
            }

            var url = new Uri(DataContextData.Url);

            Installed = true;
            var fButton = this.Get<Button>("FFinish");
            fButton.IsEnabled = false;

            this.Get<Button>("FBack").IsEnabled = false;
            Program.StartInstall(new()
            {
                Source = url,
                Path = DataContextData.Path,
                UniqueIdentifier = Guid.NewGuid() //todo: get for, install pack
            }, true);
            Close(true);
        }

        private void Back_OnClick(object? sender, RoutedEventArgs e)
        {
            Pages.SelectedIndex = (Pages.SelectedIndex - 1) % Pages.Items.Cast<object>().Count();
        }

        private async void OpenFolder_OnClick(object? sender, RoutedEventArgs e)
        {
            OpenFolderDialog dialog = new();
            var path = await dialog.ShowAsync(this);
            DataContextData.Path = path;
        }
    }
}
