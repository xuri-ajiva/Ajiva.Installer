using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Ajiva.Installer.Views
{
    public class TabMenu : UserControl
    {
        public TabMenu()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
