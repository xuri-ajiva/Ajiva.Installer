using System;
using System.Drawing;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia.Platform;
using Bitmap = Avalonia.Media.Imaging.Bitmap;

namespace Ajiva.Installer.ViewModels
{
    public class BitmapValueConverter : IValueConverter
    {
        public static BitmapValueConverter Instance = new();

        public object Convert(object? value, Type targetType, object parameter, CultureInfo culture)
        {
            // ReSharper disable once ConvertIfStatementToSwitchStatement
            if (value is string path && (targetType == typeof(IImage)))
            {
                var uri = new Uri(path, UriKind.RelativeOrAbsolute);
                var scheme = uri.IsAbsoluteUri ? uri.Scheme : "file";

                switch (scheme)
                {
                    case "file":
                        return new Bitmap(path);
                    default:
                        {
                            var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
                            return new Bitmap(assets.Open(uri));
                        }
                }
            }
            if (value is null)
            {
                return SystemIcons.Error.ToBitmap();
            }
            throw new NotSupportedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}