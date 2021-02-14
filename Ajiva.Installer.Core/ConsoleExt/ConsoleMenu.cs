using System;
using System.Linq;

namespace Ajiva.Installer.Core.ConsoleExt
{
    public class ConsoleMenu
    {
        public void ShowMenu(string title, params ConsoleMenuItem[] items)
        {
            Console.WriteLine(title);
            var formate = new string(Enumerable.Repeat('0', items.Length / 10 + 1).ToArray());
            var i = 0;
            foreach (var item in items)
            {
                Console.WriteLine($"[{i.ToString(formate)}]: {item.Name}");
                i++;
            }
            int num;
            do
            {
                Console.WriteLine("Chose Action [0...{i}]");
            } while (int.TryParse(Console.ReadLine(), out num) && num < 0 && num > i);

            items[num].Action?.Invoke();
        }
    }

    public record ConsoleMenuItem (string Name, Action? Action);
}
