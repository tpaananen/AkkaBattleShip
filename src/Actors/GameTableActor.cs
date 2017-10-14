using System;
using System.Collections.Generic;
using System.Linq;
using Akka.Actor;
using Messages.FSharp;

namespace Actors.CSharp
{
    public class GameTableActor : BattleShipActor
    {
        private readonly List<Ship> _ships = new List<Ship>();
        private readonly Dictionary<Point, IActorRef> _pointActors = new Dictionary<Point, IActorRef>(100);
        private readonly List<Point> _currentPoints = new List<Point>(100);
        private readonly Stack<TablesAndShips.Piece> _shipConfig = new Stack<TablesAndShips.Piece>(TablesAndShips.GetTablesAndShips());

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
            Receive<ShipPosition>(message => message.Token == _playerToken && message.GameToken == _gameToken, message =>
            {
                if (AddToTable(message.Ship, out var error))
                {
                    _shipConfig.Pop();
                    Context.Parent.Tell(new GameTable(_playerToken, _gameToken, _currentPoints), Self);
                }
                SendPositionRequestOrBecomeReady(error);
            });
        }

        private void GameOn()
        {
            Receive<Missile>(message =>
            {
                if (!_pointActors.TryGetValue(message.Point, out var pointActor))
                {
                    Log.Error("Invalid point " + message.Point + " received.");
                    Context.Parent.Tell(new MissileDidNotHitShip(_playerToken, _gameToken, message.Point), Self);
                    return;
                }
                pointActor.Tell(message, Self);
            });

            Receive<PartOfTheShipDestroyed>(message =>
            {
                ReportTable(message);
                Context.Parent.Tell(new MissileWasAHit(_playerToken, _gameToken, message.Point, false), Self);
            });

            Receive<ShipDestroyed>(message =>
            {
                _ships.RemoveAll(ship => ship.Points.Any(point => point == message.Point));
                ReportTable(message);
                if (_ships.Count == 0)
                {
                    Become(GameOver);
                }
                else
                {
                    Context.Parent.Tell(new MissileWasAHit(_playerToken, _gameToken, message.Point, true));
                }
            });

            Receive<MissileDidNotHitShip>(message =>
            {
                ReportTable(message);
                Context.Parent.Tell(message, Self);
            });

            Receive<AlreadyHit>(message =>
            {
                ReportTable(message);
                Context.Parent.Tell(message, Self);
            });

            Receive<WithPoint>(message =>
            {
                ReportTable(message);
            });
        }

        private void ReportTable(WithPoint message)
        {
            ReplacePoint(message);
            Context.Parent.Tell(new GameTable(_playerToken, _gameToken, _currentPoints), Self);
        }

        private void GameOver()
        {
            Context.Parent.Tell(new GameOver(_playerToken, _gameToken), Self);
        }

        private void ReplacePoint(WithPoint message)
        {
            _currentPoints[_currentPoints.IndexOf(message.Point)] = message.Point;
        }

        private void SendPositionRequestOrBecomeReady(string error)
        {
            if (_shipConfig.Count > 0)
            {
                Context.Parent.Tell(new GiveMeNextPosition(_playerToken, _gameToken, _shipConfig.Peek(), error), Self);
            }
            else
            {
                foreach (var point in _currentPoints.Where(d => !d.HasShip))
                {
                    _pointActors.Add(point, Context.ActorOf(Props.Create(() => new PointActor(point, _gameToken))));
                }
                Context.Parent.Tell(new GameStatusUpdate(_playerToken, _gameToken, GameStatus.Configured, Context.Parent, null), Self);
                Become(GameOn);
            }
        }

        private bool AddToTable(Ship ship, out string error)
        {
            var currentItem = _shipConfig.Peek();
            if (!ShipValidator.IsValid(_currentPoints, currentItem.Length, ship, out error))
            {
                return false;
            }

            var shipActor = Context.ActorOf(Props.Create(() => new ShipActor(ship, _gameToken)));
            _ships.Add(ship);
            foreach (var point in ship.Points)
            {
                _pointActors[point] = shipActor;
                _currentPoints[_currentPoints.IndexOf(point)] = point;
            }
            
            error = null;
            return true;
        }

        private void AddNonShipPoints()
        {
            for (var x = 'A'; x <= 'J'; ++x)
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
