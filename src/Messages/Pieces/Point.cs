﻿using System;

namespace Messages.CSharp.Pieces
{
    public sealed class Point : IEquatable<Point>, IComparable<Point>
    {
        public byte X { get; private set; }
        public byte Y { get; private set; }
        public bool HasShip { get; private set; }
        public bool HasHit { get; private set; }

        public Point(byte x, byte y, bool hasShip = false, bool hasHit = false)
        {
            X = x;
            Y = y;
            HasShip = hasShip;
            HasHit = hasHit;
        }

        public decimal DistanceTo(Point other)
        {
            return this - other + 1;
        }

        public int CompareTo(Point other)
        {
            if (other.Y < Y)
            {
                return 1;
            }
            return X.CompareTo(other.X);
        }

        public bool Equals(Point other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return X == other.X && Y == other.Y;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
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
            return string.Format("{0}:{1}", X, Y);
        }

        public static decimal operator -(Point left, Point right)
        {
            return (decimal)Math.Sqrt(Math.Pow(left.X - right.X, 2) + Math.Pow(left.Y - right.Y, 2));
        }

    }
}
