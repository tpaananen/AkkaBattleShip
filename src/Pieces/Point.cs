using System;

namespace Pieces
{
    public struct Point : IEquatable<Point>
    {
        public readonly byte X;

        public readonly byte Y;

        public Point(byte x, byte y)
        {
            X = x;
            Y = y;
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
            return left.Equals(right);
        }

        public static bool operator !=(Point left, Point right)
        {
            return !left.Equals(right);
        }

        public static int operator -(Point left, Point right)
        {
            if (left.X == right.X)
            {
                return Math.Abs(left.Y - right.Y);
            }
            if (left.Y == right.Y)
            {
                return Math.Abs(left.X - right.X);
            }
            throw new InvalidOperationException("Invalid point configuration.");
        }

    }
}
