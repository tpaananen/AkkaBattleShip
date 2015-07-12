using System;

namespace Messages.CSharp.Pieces
{
    public struct Point : IEquatable<Point>, IComparable<Point>
    {
        public const char A = 'A';
        public const char J = 'J';
        public char X { get; private set; }
        public byte Y { get; private set; }
        public bool HasShip { get; private set; }
        public bool HasHit { get; private set; }

        public Point(char x, byte y, bool hasShip, bool hasHit)
        {
            RequireXPositionInRange(x, "x");
            RequireYPositionInRange(y, "y");
            X = x;
            Y = y;
            HasShip = hasShip;
            HasHit = hasHit;
        }

        // ReSharper disable once UnusedParameter.Local
        private static void RequireYPositionInRange(byte value, string name)
        {
            if (value < 1 || value > 10)
            {
                throw new ArgumentOutOfRangeException(name);
            }
        }

        // ReSharper disable once UnusedParameter.Local
        private static void RequireXPositionInRange(char value, string name)
        {
            if (value < A || value > J)
            {
                throw new ArgumentOutOfRangeException(name);
            }
        }

        public decimal DistanceTo(Point other)
        {
            return this - other + 1;
        }

        public int CompareTo(Point other)
        {
            return Y == other.Y ? X.CompareTo(other.X) : Y.CompareTo(other.Y);
        }

        public bool Equals(Point other)
        {
            return X == other.X && Y == other.Y;
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
                return (X.GetHashCode()*397) ^ Y.GetHashCode();
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
            return string.Format("[{0}:{1}], Ship: {2}, Hit: {3}", X, Y, HasShip, HasHit);
        }

        public static decimal operator -(Point left, Point right)
        {
            return (decimal)Math.Sqrt(Math.Pow((left.X - A) - (right.X - A), 2) + Math.Pow(left.Y - right.Y, 2));
        }

    }
}
