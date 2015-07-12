using System;
using System.Collections.Generic;
using Messages.CSharp;
using Messages.CSharp.Pieces;

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

            Receive<Message.Missile>(message => message.GameToken == _gameToken, message =>
            {
                _points.Remove(message.Point);
                var point = PointHasHit(message.Point);
                if (_points.Count == 0)
                {
                    Context.Parent.Tell(new Message.ShipDestroyed(Guid.Empty, _gameToken, point), Self);
                    Become(Destroyed);
                }
                else
                {
                    Context.Parent.Tell(new Message.PartOfTheShipDestroyed(Guid.Empty, _gameToken, point), Self);
                }
            });
        }

        private void Destroyed()
        {
            Receive<Message.Missile>(message =>
            {
                Context.Parent.Tell(new Message.AlreadyHit(Guid.Empty, _gameToken, message.Point), Self);
            });

            ReceiveAny(message =>
            {
                Log.Error("Ship is already destroyed, not expecting any messages, but got " + message.GetType());
            });
        }

        private static Point PointHasHit(Point point)
        {
            return new Point(point.X, point.Y, point.HasShip, true);
        }

    }
}
