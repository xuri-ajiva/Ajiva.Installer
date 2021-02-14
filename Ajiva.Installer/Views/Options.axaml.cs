using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Ajiva.Installer.Views
{
    public class Options : UserControl
    {
        public Options()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
