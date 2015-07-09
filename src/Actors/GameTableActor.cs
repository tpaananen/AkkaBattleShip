using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Messages.CSharp;
using Messages.CSharp.Pieces;

namespace Actors.CSharp
{
    public class GameTableActor : BattleShipActor
    {
        private readonly HashSet<IActorRef> _shipActors = new HashSet<IActorRef>();
        private readonly Dictionary<Point, IActorRef> _pointActors = new Dictionary<Point, IActorRef>(); 
        private readonly List<Point> _currentPoints = new List<Point>(); 

        public GameTableActor(IReadOnlyList<Ship> ships)
        {
            IntializeTable(ships);
            Become(GameOn);
        }

        private void GameOn()
        {
            Receive<MessageMissile>(message =>
            {
                _pointActors[message.Point].Tell(message, Self);
            });

            Receive<MessageShipDestroyed>(message => _shipActors.Contains(Sender), message =>
            {
                _shipActors.Remove(Sender);
                if (_shipActors.Count == 0)
                {
                    Become(GameOver);
                }
            });

            Receive<MessageAlreadyHit>(message =>
            {
                Context.Parent.Tell(message, Self);
            });

            Receive<MessageWithPoint>(message =>
            {
                ReplacePoint(message);
                Context.Parent.Tell(ConstructTableStatusMessage(), Self);
            });
        }

        private void GameOver()
        {
            Context.Parent.Tell(new MessageGameOver(), Self);

            ReceiveAny(message =>
            {
                Log.Error("Message received while game over");
            });
        }

        private void ReplacePoint(MessageWithPoint message)
        {
            // point compares only X and Y, so we can use the new point to remove the old one with the same coords
            _currentPoints.Remove(message.Point);
            _currentPoints.Add(message.Point);
        }

        private Point[] ConstructTableStatusMessage()
        {
            _currentPoints.Sort();
            return _currentPoints.ToArray();
        }

        private void IntializeTable(IReadOnlyList<Ship> ships)
        {
            var pointsWithShip = ships.SelectMany(x => x.Points).ToList();

            for (byte y = 1; y <= 10; ++y)
            {
                for (byte x = 1; x <= 10; ++x)
                {
                    var point = new Point(x, y, pointsWithShip.Any(d => d.X == x && d.Y == y));
                    _currentPoints.Add(point);
                    _pointActors.Add(point, Context.ActorOf(Props.Create(() => new PointActor(point)), point.ToString()));
                }
            }

            foreach (var ship in ships)
            {
                var shipActor = Context.ActorOf(Props.Create(() => new ShipActor(ship)));
                foreach (var point in ship.Points)
                {
                    _pointActors[point].Tell(new MessageYouArePartOfShip(shipActor));
                }
                _shipActors.Add(shipActor);
            }
        }
    }
}
