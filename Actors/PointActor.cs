using Akka.Actor;
using Messages.CSharp;
using Messages.CSharp.Pieces;

namespace Actors.CSharp
{
    public class PointActor : BattleShipActor
    {
        private Point _point;

        private IActorRef _ship;

        public PointActor(Point point)
        {
            _point = point;

            Receive<MessageYouArePartOfShip>(message =>
            {
                _ship = message.ShipActorRef;
            });

            Receive<MessageMissile>(message => message.Point == _point, message =>
            {
                PointHasHit();
                if (_ship != null)
                {
                    _ship.Tell(new MessagePartOfTheShipDestroyed(_point), Self);
                }
                else
                {
                    Context.Parent.Tell(new MessageMissileDidNotHitShip(message.Point), Self);
                }
                Become(Destroyed);
            });
        }

        private void Destroyed()
        {
            Receive<MessageMissile>(message =>
            {
                Context.Parent.Tell(new MessageAlreadyHit(_point), Self);
            });
        }

        private void PointHasHit()
        {
            _point = new Point(_point.X, _point.Y, _point.HasShip, true);
        }
    }
}
