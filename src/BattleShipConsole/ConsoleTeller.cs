using System;

namespace BattleShipConsole
{
    public abstract class ConsoleTeller
    {
        protected static void Tell(string message)
        {
            Console.WriteLine(message);
        }

        protected static void Tell()
        {
            Console.WriteLine();
        }
    }
}
