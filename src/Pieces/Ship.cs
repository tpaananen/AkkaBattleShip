namespace Pieces
{
    public class Ship
    {
        public Point StartPoint { get; private set; }

        public Point EndPoint { get; private set; }

        public int Length { get; private set; }

        public Ship(Point start, Point end)
        {
            Length = start - end;
            StartPoint = start;
            EndPoint = end;
        }
    }
}
