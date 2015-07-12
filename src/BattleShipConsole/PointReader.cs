using System;
using System.Collections.Generic;
using System.Linq;
using Messages.CSharp.Pieces;

namespace BattleShipConsole
{
    public class PointReader : ConsoleTeller
    {
        public bool ParsePoint(string start, out Point point)
        {
            point = new Point('A', 1, true, false);
            if (start.Length < 2 || start.Length > 3)
            {
                Tell("Invalid number of characters in " + start);
                return false;
            }

            var c = start.Substring(0, 1).ToUpper()[0];
            var n = byte.Parse(start.Substring(1));

            if (c < Point.A || c > Point.J)
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

        public bool CreateShip(string coords, int len, ICollection<Ship> selectedShips)
        {
            var split = coords.Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
            if (split.Length != 2)
            {
                return false;
            }

            Point startPoint;
            if (!ParsePoint(split[0], out startPoint))
            {
                return false;
            }

            Point endPoint;
            if (!ParsePoint(split[1], out endPoint))
            {
                return false;
            }

            var distance = startPoint.DistanceTo(endPoint);
            if (distance != len)
            {
                Tell("Length of the ship is not " + len + ", but " + distance);
                return false;
            }

            return FillAndPushPoints(startPoint, endPoint, selectedShips);
        }

        private static bool FillAndPushPoints(Point startPoint, Point endPoint, ICollection<Ship> selectedShips)
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

                var points = selectedShips.SelectMany(x => x.Points);
                foreach (var point in points)
                {
                    if (list.Contains(point))
                    {
                        Tell("The point " + point + " already exists, overlapping ships are not allowed.");
                        return false;
                    }
                }
            }

            list.Sort();
            var ship = new Ship(list);
            selectedShips.Add(ship);
            return true;
        }
    }
}
