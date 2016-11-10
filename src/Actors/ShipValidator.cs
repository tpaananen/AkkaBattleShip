using System.Collections.Generic;
using System.Linq;
using Messages.CSharp.Pieces;

namespace Actors.CSharp
{
    internal static class ShipValidator
    {
        public static bool IsValid(IReadOnlyList<Point> allPoints, int currentShipLength, Ship ship, out string error)
        {
            if (ship.Length != currentShipLength)
            {
                error = "The given ship length is not " + currentShipLength;
                return false;
            }

            if (allPoints.Where(d => d.HasShip).Intersect(ship.Points).Any())
            {
                error = "The given ship overlaps with the existing ship.";
                return false; // point already exists
            }

            if (ship.Points.Any(point => HasShipNextDoor(allPoints, point)))
            {
                error = "The ship is next to another ship.";
                return false;
            }
            error = null;
            return true;
        }

        private static bool HasShipNextDoor(IEnumerable<Point> points, Point point)
        {
            var list = new[]
            {
                new KeyValuePair<char, int>((char)(point.X - 1), point.Y),
                new KeyValuePair<char, int>((char)(point.X + 1), point.Y),
                new KeyValuePair<char, int>((char)point.X, (byte)(point.Y - 1)),
                new KeyValuePair<char, int>((char)point.X, (byte)(point.Y + 1))
            };
            return points.Where(d => d.HasShip).Any(c => list.Any(d => d.Key == c.X && d.Value == c.Y));
        }
    }
}
