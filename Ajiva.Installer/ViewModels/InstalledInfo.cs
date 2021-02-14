using System.Diagnostics;
using ReactiveUI;

namespace Ajiva.Installer.ViewModels
{
    public class InstalledInfo : ReactiveObject
    {
        private double progress;
        private int widthIcon = 60;
        private int widthText = 300;
        private string name;
        private string description;
        private string iconSrc;
        private string path;

        private readonly ExecutingOptions executingOptions = new();
        public int WidthIcon
        {
            get => widthIcon;
            set => this.RaiseAndSetIfChanged(ref widthIcon, value);
        }
        public int WidthText
        {
            get => widthText;
            set => this.RaiseAndSetIfChanged(ref widthText, value);
        }

        public string Name
        {
            get => name;
            set => this.RaiseAndSetIfChanged(ref name, value);
        }
        public string Description
        {
            get => description;
            set => this.RaiseAndSetIfChanged(ref description, value);
        }
        public string IconSrc
        {
            get => iconSrc;
            set => this.RaiseAndSetIfChanged(ref iconSrc, value);
        }

        public string Path
        {
            get => path;
            set => this.RaiseAndSetIfChanged(ref path, value);
        }

        public ExecutingOptions ExecutingOptions => executingOptions;

        public double Progress
        {
            get => progress;
            set => this.RaiseAndSetIfChanged(ref progress, value);
        }

        public ProcessStartInfo StartInfo()
        {
            var exe = ExecutingOptions.Executable.ReplaceDynamic(this);
            var args = ExecutingOptions.Args.ReplaceDynamic(this);
            return new()
            {
                Arguments = $"/c echo ----------------- && echo Starting: \"{Name}\" && echo \"{exe} {args}\" && echo. && echo ----------------- && \"{exe}\" {args}",
                FileName = "cmd.exe",
                WorkingDirectory = ExecutingOptions.WorkDirectory.ReplaceDynamic(this),
                UseShellExecute = true,
            };
        }
    }
}