using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Ajiva.Installer.ViewModels;

namespace Ajiva.Installer.Helpers
{
    internal static class Interop
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern bool AttachConsole(uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        internal static extern bool FreeConsole();

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool FlashWindow(IntPtr hWnd, bool invert);

        [DllImport("kernel32.dll")]
        internal static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        internal static extern bool ShowWindow(IntPtr hWnd, ShowWindowCmd nCmdShow);

        internal enum ShowWindowCmd
        {
            SW_FORCEMINIMIZE = 11, SW_HIDE = 0, SW_MAXIMIZE = 3, SW_MINIMIZE = 6,
            SW_RESTORE = 9, SW_SHOW = 5, SW_SHOWDEFAULT = 10, SW_SHOWMAXIMIZED = 3,
            SW_SHOWMINIMIZED = 2, SW_SHOWMINNOACTIVE = 7, SW_SHOWNA = 8,
            SW_SHOWNOACTIVATE = 4, SW_SHOWNORMAL = 1,
        }

        internal static class Console
        {
            internal static IntPtr ConsolePtr;

            public static void Show()
            {
                AllocConsole();
            }

            public static void Hide()
            {
                FreeConsole();
            }
        }
    }
}
