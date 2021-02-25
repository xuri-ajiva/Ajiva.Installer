using System.Diagnostics;
using System.Text.Json.Serialization;
using ReactiveUI;

namespace Ajiva.Installer.ViewModels
{
    public class InstalledInfo : ReactiveObject
    {
        private double progress = 0;
        private int widthIcon = 60;
        private int widthText = 300;
        private string? name;
        private string? description;
        private string? iconSrc;
        private string? path;
        private AvailableAction availableAction;
        private Uri? source;
        
        public Guid UniqueIdentifier { get; set; }

        public AvailableAction AvailableAction
        {
            get => availableAction;
            set => this.RaiseAndSetIfChanged(ref availableAction, value);
        }

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
            set
            {
                this.RaiseAndSetIfChanged(ref iconSrc, value);
                this.RaisePropertyChanged(nameof(IconDynamic));
            }
        }

        [JsonIgnore]
        public string IconDynamic => iconSrc.ReplaceDynamic(this);

        public string Path
        {
            get => path;
            set => this.RaiseAndSetIfChanged(ref path, value);
        }

        public ExecutingOptions ExecutingOptions { get; set; } = new();

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
