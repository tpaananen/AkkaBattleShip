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
        private readonly Dictionary<Point, IActorRef> _pointActors = new Dictionary<Point, IActorRef>(100); 
        private readonly List<Point> _currentPoints = new List<Point>(100);
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
            Receive<Message.Missile>(message =>
            {
                IActorRef pointActor;
                if (!_pointActors.TryGetValue(message.Point, out pointActor))
                {
                    Log.Error("Invalid point " + message.Point + " received.");
                    Context.Parent.Tell(new Message.MissileDidNotHitShip(Guid.Empty, _gameToken, message.Point), Self);
                    return;
                }
                pointActor.Tell(message, Self);
            });

            Receive<Message.PartOfTheShipDestroyed>(message =>
            {
                ReportTable(message);
                Context.Parent.Tell(new Message.MissileWasAHit(Guid.Empty, _gameToken, message.Point), Self);
            });

            Receive<Message.ShipDestroyed>(message =>
            {
                _ships.RemoveAll(ship => ship.Points.Any(point => point == message.Point));
                ReportTable(message);
                if (_ships.Count == 0)
                {
                    Become(GameOver);
                }
                else
                {
                    Context.Parent.Tell(new Message.MissileWasAHit(Guid.Empty, _gameToken, message.Point, true));
                }
            });

            Receive<Message.MissileDidNotHitShip>(message =>
            {
                ReportTable(message);
                Context.Parent.Tell(message, Self);
            });

            Receive<Message.AlreadyHit>(message =>
            {
                ReportTable(message);
                Context.Parent.Tell(message, Self);
            });

            Receive<Message.WithPoint>(message =>
            {
                ReportTable(message);
            });
        }

        private void ReportTable(Message.WithPoint message)
        {
            ReplacePoint(message);
            Context.Parent.Tell(ConstructTableStatusMessage(), Self);
        }

        private void GameOver()
        {
            Context.Parent.Tell(new Message.GameOver(Guid.Empty, _gameToken), Self);
            ReceiveAny(message =>
            {
                Log.Error("Message received while game over");
            });
        }

        private void ReplacePoint(Message.WithPoint message)
        {
            _currentPoints[_currentPoints.IndexOf(message.Point)] = message.Point;
        }

        private IReadOnlyList<Point> ConstructTableStatusMessage()
        {
            return _currentPoints;
        }

        private void IntializeTable(IEnumerable<Ship> ships)
        {
            foreach (var ship in ships)
            {
                var shipActor = Context.ActorOf(Props.Create(() => new ShipActor(ship, _gameToken)));
                foreach (var point in ship.Points)
                {
                    _pointActors[point] = shipActor;
                }
                _currentPoints.AddRange(ship.Points);
            }

            for (byte y = 1; y <= 10; ++y)
            {
                for (var x = Point.A; x <= Point.J; ++x)
                {
                    var point = new Point(x, y, false, false);
                    if (!_pointActors.ContainsKey(point))
                    {
                        _pointActors.Add(point, Context.ActorOf(Props.Create(() => new PointActor(point, _gameToken))));
                        _currentPoints.Add(point);
                    }
                }
            }
            _currentPoints.Sort();
        }
    }
}
