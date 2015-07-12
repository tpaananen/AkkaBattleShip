using System;
using Messages.CSharp.Pieces;

namespace BattleShipConsole
{
    public class TableWriter : ConsoleTeller
    {
        public void ShowTable(Point[] points)
        {
            Tell("Battleships:");
            WriteHorizontalCharacterRow();

            for (int i = 0; i < 10; ++i)
            {
                WriteLineNumber(i);
                for (int j = 0; j < 10; ++j)
                {
                    var index = i * 10 + j;
                    var point = points[index];

                    if (point.HasShip && !point.HasHit)
                    {
                        Console.BackgroundColor = ConsoleColor.Gray;
                    }
                    else if (point.HasShip && point.HasHit)
                    {
                        Console.BackgroundColor = ConsoleColor.DarkMagenta;
                    }
                    else
                    {
                        Console.ResetColor();
                    }
                    Console.Write(point.HasHit ? "X" : " ");
                }
                Console.ResetColor();
                Tell();
            }
            Console.ResetColor();
        }

        private static void WriteHorizontalCharacterRow()
        {
            Console.Write("  ");
            for (var i = 'A'; i <= 'J'; ++i)
            {
                Console.Write(i);
            }
            Tell();
        }

        private static void WriteLineNumber(int i)
        {
            Console.Write((i + 1).ToString().PadLeft(2, ' '));
        }
    }
}
