using System;

namespace Messages.CSharp.Pieces
{
    public struct Point : IEquatable<Point>, IComparable<Point>
    {
        // Workaround: Wire does not know how to play with chars
        private readonly int _fieldX;

        public char X => (char)_fieldX;

        public int Y { get; private set; }
        public bool HasShip { get; private set; }
        public bool HasHit { get; private set; }

        public Point(int x, int y, bool hasShip, bool hasHit)
        {
            RequireXPositionInRange(x, "x");
            RequireYPositionInRange(y, "y");
            _fieldX = x;
            Y = y;
            HasShip = hasShip;
            HasHit = hasHit;
        }

        // ReSharper disable once UnusedParameter.Local
        private static void RequireYPositionInRange(int value, string name)
        {
            if (value < 1 || value > 10)
            {
                throw new ArgumentOutOfRangeException(name);
            }
        }

        // ReSharper disable once UnusedParameter.Local
        private static void RequireXPositionInRange(int value, string name)
        {
            if (value < 'A' || value > 'J')
            {
                throw new ArgumentOutOfRangeException(name);
            }
        }

        public decimal DistanceTo(Point other)
        {
            if (this == other)
            {
                return 1;
            }
            return this - other + 1;
        }

        public int CompareTo(Point other)
        {
            return Y == other.Y ? _fieldX.CompareTo(other._fieldX) : Y.CompareTo(other.Y);
        }

        public bool Equals(Point other)
        {
            return _fieldX == other._fieldX && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Point && Equals((Point) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (_fieldX.GetHashCode()*397) ^ Y.GetHashCode();
            }
        }

        public static bool operator ==(Point left, Point right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(Point left, Point right)
        {
            return !Equals(left, right);
        }

        public override string ToString()
        {
            return $"[{X}:{Y}], Ship: {HasShip}, Hit: {HasHit}";
        }

        public static decimal operator -(Point left, Point right)
        {
            return (decimal)Math.Sqrt(Math.Pow((left.X - 'A') - (right.X - 'A'), 2) + Math.Pow(left.Y - right.Y, 2));
        }

    }
}
