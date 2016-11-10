using System;
using System.Collections.Generic;
using Messages.CSharp.Pieces;

namespace BattleShipConsole
{
    public class PointReader : ConsoleTeller
    {
        public static bool ParsePoint(string start, out Point point)
        {
            point = new Point('A', 1, true, false);
            if (start.Length < 2 || start.Length > 3)
            {
                Tell("Invalid number of characters in " + start);
                return false;
            }

            var c = start.Substring(0, 1).ToUpper()[0];
            var n = int.Parse(start.Substring(1));

            if (c < 'A' || c > 'J')
            {
                Tell("Invalid range in " + start);
                return false;
            }

            if (n < 1 || n > 10)
            {
                Tell("Invalid range in " + start);
                return false;
            }

            point = new Point(c, n, true, false);
            return true;
        }

        public Ship CreateShip(string coords)
        {
            var split = coords.Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
            if (split.Length != 2)
            {
                return null;
            }

            Point startPoint;
            if (!ParsePoint(split[0], out startPoint))
            {
                return null;
            }

            Point endPoint;
            if (!ParsePoint(split[1], out endPoint))
            {
                return null;
            }

            return CreateShip(startPoint, endPoint);
        }

        private static Ship CreateShip(Point startPoint, Point endPoint)
        {
            var list = new List<Point> { startPoint };
            if (startPoint != endPoint)
            {
                list.Add(endPoint);
            }
            list.Sort();

            if (list.Count > 1 && startPoint.DistanceTo(endPoint) > 2)
            {
                if (list[0].X == list[1].X)
                {
                    // vertical
                    for (var y = (byte)(list[0].Y + 1); y < list[1].Y; ++y)
                    {
                        list.Add(new Point(list[0].X, y, true, false));
                    }
                }
                else
                {
                    // Horiz
                    for (var x = (char)(list[0].X + 1); x < list[1].X; ++x)
                    {
                        list.Add(new Point(x, list[0].Y, true, false));
                    }
                }
            }

            list.Sort();
            return new Ship(list.ToArray());
        }
    }
}
