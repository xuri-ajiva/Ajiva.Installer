using System.Linq;
using Ajiva.Installer.ViewModels;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Metadata;

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
    }

    public class ContendTemp
    {
        [Content]
        public IControl Main { get; set; } = null!;
    }
}
