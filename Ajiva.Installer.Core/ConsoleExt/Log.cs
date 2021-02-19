using System;

namespace Ajiva.Installer.Core.ConsoleExt
{
    public class LogHelper
    {
        public static void Log(object obj)
        {
            lock (WriteLock)
                Console.WriteLine(obj.ToString());
        }

        public static void LogNoBreak(object obj)
        {
            lock (WriteLock)
                Console.Write(obj.ToString());
        }

        public static void WriteLine(object obj) => Log(obj);

        public static void Write(string msg, int position)
        {
            lock (WriteLock)
            {
                var (left, top) = Console.GetCursorPosition();

                if (top != position) Console.SetCursorPosition(0, position);

                Console.Write(msg);

                if (top != position) Console.SetCursorPosition(left, top);
            }
        }

        internal static readonly object WriteLock = new();

        public static string GetInput(string message)
        {
            Console.WriteLine(message);
            return Console.ReadLine() ?? "";
        }

        private static ConsoleBlock? YesNo;

        public static bool YesNow(string s)
        {
            lock (WriteLock)
            {
                var (left, top) = Console.GetCursorPosition();
                var msg = s + " [y/n]";
                YesNo ??= new(1);
                while (true)
                {
                    YesNo.WriteAtNoBreak(msg, 0);
                    Console.SetCursorPosition(msg.Length + 1, YesNo.Top);
                    var key = Console.ReadKey();
                    // ReSharper disable once SwitchStatementHandlesSomeKnownEnumValuesWithDefault
                    switch (key.Key)
                    {
                        case ConsoleKey.Y:
                            Console.SetCursorPosition(left, top + 1);
                            return true;
                        case ConsoleKey.N:
                            Console.SetCursorPosition(left, top + 1);
                            return false;
                        default:
                            msg = s + " [Y/N]! ";
                            break;
                    }
                }
            }
        }
    }
    public class ConsoleBlock
    {
        public readonly int Count;
        public readonly int Top;

        public ConsoleBlock(int count)
        {
            lock (LogHelper.WriteLock)
            {
                this.Count = count;
                Top = Console.CursorTop;
                Console.SetCursorPosition(0, Top + count);
            }
        }

        public void WriteAt(string msg, int position)
        {
            if (position > Count) throw new ArgumentOutOfRangeException(nameof(position), position, "expected les than count: " + Count);
            LogHelper.Write(msg.FillUp(Console.BufferWidth), Top + position);
        }

        public void WriteAtNoBreak(string msg, int position)
        {
            if (position > Count) throw new ArgumentOutOfRangeException(nameof(position), position, "expected les than count: " + Count);
            LogHelper.Write("".FillUp(Console.BufferWidth), Top + position);
            LogHelper.Write(msg, Top + position);
        }
    }

    public class ConsoleRolBlock
    {
        private ConsoleBlock Block;
        private int index = 0;

        public ConsoleRolBlock(int count)
        {
            Block = new(count);
        }

        public void WriteNext(string msg)
        {
            Block.WriteAt($"[{index:X4}] " + msg, index++ % Block.Count);
        }
    }
}
