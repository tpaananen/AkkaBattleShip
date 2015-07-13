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
        private readonly List<Ship> _ships = new List<Ship>();
        private readonly Dictionary<Point, IActorRef> _pointActors = new Dictionary<Point, IActorRef>(100); 
        private readonly List<Point> _currentPoints = new List<Point>(100);
        private Stack<Tuple<string, int>> _shipConfig = new Stack<Tuple<string, int>>(TablesAndShips.Ships);

        private readonly Guid _playerToken;
        private readonly Guid _gameToken;

        public GameTableActor(Guid playerToken, Guid gameToken)
        {
            _playerToken = playerToken;
            _gameToken = gameToken;
            AddNonShipPoints();
            SendPositionRequestOrBecomeReady(null);
            Become(Configuring);
        }

        private void Configuring()
        {
            Receive<Message.ShipPosition>(message => message.Token == _playerToken && message.GameToken == _gameToken, message =>
            {
                string error;
                if (AddToTable(message.Ship, out error))
                {
                    _shipConfig.Pop();
                    Context.Parent.Tell(new Message.GameTable(_playerToken, _gameToken, _currentPoints), Self);
                }
                SendPositionRequestOrBecomeReady(error);
            });
        }

        private void GameOn()
        {
            Receive<Message.Missile>(message =>
            {
                IActorRef pointActor;
                if (!_pointActors.TryGetValue(message.Point, out pointActor))
                {
                    Log.Error("Invalid point " + message.Point + " received.");
                    Context.Parent.Tell(new Message.MissileDidNotHitShip(_playerToken, _gameToken, message.Point), Self);
                    return;
                }
                pointActor.Tell(message, Self);
            });

            Receive<Message.PartOfTheShipDestroyed>(message =>
            {
                ReportTable(message);
                Context.Parent.Tell(new Message.MissileWasAHit(_playerToken, _gameToken, message.Point), Self);
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
                    Context.Parent.Tell(new Message.MissileWasAHit(_playerToken, _gameToken, message.Point, true));
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
            Context.Parent.Tell(new Message.GameTable(_playerToken, _gameToken, _currentPoints), Self);
        }

        private void GameOver()
        {
            Context.Parent.Tell(new Message.GameOver(_playerToken, _gameToken), Self);
        }

        private void ReplacePoint(Message.WithPoint message)
        {
            _currentPoints[_currentPoints.IndexOf(message.Point)] = message.Point;
        }

        private void SendPositionRequestOrBecomeReady(string error)
        {
            if (_shipConfig.Count > 0)
            {
                Context.Parent.Tell(new Message.GiveMeNextPosition(_playerToken, _gameToken, _shipConfig.Peek(), error), Self);
            }
            else
            {
                _shipConfig = null;
                foreach (var point in _currentPoints.Where(d => !d.HasShip))
                {
                    _pointActors.Add(point, Context.ActorOf(Props.Create(() => new PointActor(point, _gameToken))));
                }
                Context.Parent.Tell(new Message.GameStatusUpdate(_playerToken, _gameToken, GameStatus.Configured, null), Self);
                Become(GameOn);
            }
        }

        private bool AddToTable(Ship ship, out string error)
        {
            var currentItem = _shipConfig.Peek();
            if (ship.Length != currentItem.Item2)
            {
                error = "The given ship length is not " + currentItem.Item1;
                return false;
            }

            if (_currentPoints.Where(d => d.HasShip).Intersect(ship.Points).Any())
            {
                error = "The given ship overlaps with the existing ship.";
                return false; // point already exists
            }

            if (HasShipNextDoor(ship.Points))
            {
                error = "The ship is next to another ship.";
                return false;
            }

            var shipActor = Context.ActorOf(Props.Create(() => new ShipActor(ship, _gameToken)));
            foreach (var point in ship.Points)
            {
                _pointActors[point] = shipActor;
            }

            _ships.Add(ship);
            foreach (var point in ship.Points)
            {
                _currentPoints[_currentPoints.IndexOf(point)] = point;
            }
            error = null;
            return true;
        }

        private bool HasShipNextDoor(IEnumerable<Point> points)
        {
            return points.Any(HasShipNextDoor);
        }

        private bool HasShipNextDoor(Point point)
        {
            var list = new []
            {
                new KeyValuePair<char, byte>((char)(point.X - 1), point.Y),
                new KeyValuePair<char, byte>((char)(point.X + 1), point.Y),
                new KeyValuePair<char, byte>(point.X, (byte)(point.Y - 1)),
                new KeyValuePair<char, byte>(point.X, (byte)(point.Y + 1))
            };
            return _currentPoints.Where(d => d.HasShip).Any(c => list.Any(d => d.Key == c.X && d.Value == c.Y));
        }

        private void AddNonShipPoints()
        {
            for (var x = Point.A; x <= Point.J; ++x)
            {
                for (byte y = 1; y <= 10; ++y)
                {
                    var point = new Point(x, y, false, false);
                    _currentPoints.Add(point);
                }
            }
            _currentPoints.Sort();
        }

        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new AllForOneStrategy(x => Directive.Escalate);
        }
    }
}
