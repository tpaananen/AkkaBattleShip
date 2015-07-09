using System;
using System.Collections.Generic;
using System.Linq;

namespace Messages.CSharp.Pieces
{
    public class Ship
    {
        public IReadOnlyList<Point> Points { get; private set; } 

        public Ship(IReadOnlyList<Point> points)
        {
            Points = points;
            ValidatePoints();
        }

        private void ValidatePoints()
        {
            var horizLen = Points.Select(x => x.X).Distinct().Count();
            var vertiLen = Points.Select(y => y.Y).Distinct().Count();

            if (horizLen != 1 && vertiLen != 1)
            {
                throw new InvalidOperationException("Ships must be either vertical or horizontal.");
            }

            var selector = horizLen == 1 ? (Func<Point, int>) (point => point.Y) : (point => point.X);
            var points = Points.Select(selector).OrderBy(d => d).ToArray();
            var previous = points[0];
            for (var i = 1; i < points.Length; ++i)
            {
                var current = points[i];
                if (Math.Abs(current - previous) != 1)
                {
                    throw new InvalidOperationException("Ships must not have holes.");
                }
                previous = current;
            }
        }
    }
}
