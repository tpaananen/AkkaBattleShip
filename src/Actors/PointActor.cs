using System;
using Messages.CSharp;
using Messages.CSharp.Pieces;

namespace Actors.CSharp
{
    public class PointActor : BattleShipActor
    {
        private Point _point;
        private readonly Guid _gameToken;

        public PointActor(Point point, Guid gameToken)
        {
            _point = point;
            _gameToken = gameToken;

            Receive<Message.Missile>(message => message.Point == _point, message =>
            {
                PointHasHit();
                Context.Parent.Tell(new Message.MissileDidNotHitShip(Guid.Empty, _gameToken, _point), Self);
                Become(Destroyed);
            });
        }

        private void Destroyed()
        {
            Receive<Message.Missile>(message =>
            {
                Context.Parent.Tell(new Message.AlreadyHit(Guid.Empty, _gameToken, _point), Self);
            });
        }

        private void PointHasHit()
        {
            _point = new Point(_point.X, _point.Y, false, true);
        }
    }
}
