using System;
using Ajiva.Installer.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace Ajiva.Installer.Views
{
    public class InstalledList : UserControl
    {
        public event Action<InstalledInfo>? OnOptions;

        public InstalledList()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void Start_Click(object? sender, RoutedEventArgs e)
        {
            if ((sender as ContentControl)!.DataContext is not InstalledInfo dc) return;

            Program.StartInstalled(dc);
        }

        private void Options_Click(object? sender, RoutedEventArgs e)
        {
            if ((sender as ContentControl)!.DataContext is not InstalledInfo dc) return;

            OnOptions?.Invoke(dc);
        } 
    }
}
