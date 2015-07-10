using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Messages.CSharp;
using Messages.CSharp.Pieces;

namespace Actors.CSharp
{
    public class GameTableActor : BattleShipActor
    {
        private readonly List<Ship> _ships; 
        private readonly Dictionary<Point, IActorRef> _pointActors = new Dictionary<Point, IActorRef>(); 
        private readonly List<Point> _currentPoints = new List<Point>();
        private readonly Guid _gameToken;

        public GameTableActor(Guid gameToken, IReadOnlyList<Ship> ships)
        {
            _ships = new List<Ship>(ships);
            _gameToken = gameToken;
            IntializeTable(ships);
            Become(GameOn);
        }

        private void GameOn()
        {
            Receive<MessageMissile>(message =>
            {
                IActorRef pointActor;
                if (!_pointActors.TryGetValue(message.Point, out pointActor))
                {
                    Log.Error("Invalid point " + message.Point + " received.");
                    // TODO: return something so that game will go on
                    return;
                }
                pointActor.Tell(message, Self);
            });

            Receive<MessagePartOfTheShipDestroyed>(message =>
            {
                Context.Parent.Tell(new MessageMissileWasAHit(Guid.Empty, _gameToken, message.Point), Self);
            });

            Receive<MessageShipDestroyed>(message =>
            {
                _ships.RemoveAll(ship => ship.Points.Any(point => point == message.Point));
                if (_ships.Count == 0)
                {
                    Become(GameOver);
                }
                else
                {
                    Context.Parent.Tell(new MessageMissileWasAHit(Guid.Empty, _gameToken, message.Point, true));
                }
            });

            Receive<MessageMissileDidNotHitShip>(message =>
            {
                Context.Parent.Tell(message, Self);
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
            Context.Parent.Tell(new MessageGameOver(Guid.Empty, _gameToken), Self);

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

            foreach (var ship in ships)
            {
                var shipActor = Context.ActorOf(Props.Create(() => new ShipActor(ship, _gameToken)));
                foreach (var point in ship.Points)
                {
                    _pointActors[point] = shipActor;
                }
            }

            for (byte y = 1; y <= 10; ++y)
            {
                for (byte x = 1; x <= 10; ++x)
                {
                    var point = new Point(x, y);
                    if (!_pointActors.ContainsKey(point))
                    {
                        _pointActors.Add(point, Context.ActorOf(Props.Create(() => new PointActor(point, _gameToken)), point.ToString()));
                    }
                }
            }

            
        }
    }
}
