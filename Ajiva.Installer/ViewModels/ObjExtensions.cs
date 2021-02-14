using System;

namespace Ajiva.Installer.ViewModels
{
    internal static class ObjExtensions
    {
        public static object GetPropValue(this object src, string propName)
        {
            return src.GetType().GetProperty(propName).GetValue(src, null);
        }

        public static bool TryGetPropValue<T>(this object src, string propName, out T value)
        {
            var prop = src.GetType().GetProperty(propName);
            if (prop is not null)
            {
                value = (T)prop.GetValue(src)!;

                return true;
            }
            value = default!;
            return false;
        }

        public static string ReplaceDynamic(this string src, object obj)
        {
            if (string.IsNullOrEmpty(src)) return "";

            const string specialSing = "$";

            var pos = 0;
            while (true)
            {
                var a = src.IndexOf(specialSing, pos, StringComparison.InvariantCultureIgnoreCase);
                if (a == -1) break;
                var b = src.IndexOf(specialSing, a + specialSing.Length, StringComparison.InvariantCultureIgnoreCase);

                if (b == -1) break;

                var param = src.Substring(a + specialSing.Length, b - a - specialSing.Length);
                if (!obj.TryGetPropValue<string>(param, out var value)) continue;

                pos += a - pos + value.Length;
                src = src.Replace( /*param*/ $"{specialSing}{param}{specialSing}", value, StringComparison.InvariantCulture);

                // pos += b - pos + 1;
            }

            return src.Replace(specialSing, "");
        }
    }
}