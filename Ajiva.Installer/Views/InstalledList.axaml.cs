using System;
using Ajiva.Installer.Helpers;
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

        private void Invoke_Click(object? sender, RoutedEventArgs e)
        {
            if ((sender as ContentControl)!.DataContext is not InstalledInfo dc) return;

            switch (dc.AvailableAction)
            {
                case AvailableAction.Start:
                    RunHelper.StartInstalled(dc);
                    dc.AvailableAction = AvailableAction.Stop;
                    break;
                case AvailableAction.Install:
                    Program.StartInstall(dc, false);
                    break;
                case AvailableAction.Installing:
                    break;
                case AvailableAction.Stop:
                    RunHelper.TerminateRunning(dc);
                    dc.AvailableAction = AvailableAction.Start;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void Options_Click(object? sender, RoutedEventArgs e)
        {
            if ((sender as ContentControl)!.DataContext is not InstalledInfo dc) return;
            OnOptions?.Invoke(dc);
        }
    }
}
