using System;
using System.Collections.Generic;
using Messages.FSharp.Message;
using Messages.FSharp.Pieces;

namespace Actors.CSharp
{
    public class ShipActor : BattleShipActor
    {
        private readonly HashSet<Point> _points;
        private readonly Guid _gameToken;

        public ShipActor(Ship ship, Guid gameToken)
        {
            _gameToken = gameToken;
            _points = new HashSet<Point>(ship.Points);

            Receive<Missile>(message => message.GameToken == _gameToken, message =>
            {
                _points.Remove(message.Point);
                var point = PointHasHit(message.Point);
                if (_points.Count == 0)
                {
                    Context.Parent.Tell(new ShipDestroyed(Guid.Empty, _gameToken, point), Self);
                    Become(Destroyed);
                }
                else
                {
                    Context.Parent.Tell(new PartOfTheShipDestroyed(Guid.Empty, _gameToken, point), Self);
                }
            });
        }

        private void Destroyed()
        {
            Receive<Missile>(message =>
            {
                var point = PointHasHit(message.Point);
                Context.Parent.Tell(new AlreadyHit(Guid.Empty, _gameToken, point), Self);
            });

            ReceiveAny(message =>
            {
                Log.Error("Ship is already destroyed, not expecting any messages, but got " + message.GetType());
            });
        }

        private static Point PointHasHit(Point point)
        {
            return new Point(point.X, point.Y, true, true);
        }

    }
}
