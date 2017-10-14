using System;
using System.Collections.Generic;
using Akka.Actor;
using Messages.FSharp;

// ReSharper disable PossibleUnintendedReferenceComparison

namespace Actors.CSharp
{
    public class GameActor : BattleShipActor
    {
        private PlayerContainer _current;
        private PlayerContainer _opponent;
        private readonly Guid _gameToken;
        private bool _stopHandled;

        public GameActor(Guid gameToken)
        {
            _gameToken = gameToken;
            
            Receive<PlayerJoining>(IsForMe, message =>
            {
                var player = new PlayerContainer(message.Player);
                
                GameLog("Player " + message.Player.Name + " arrived");
                if (_current == null)
                {
                    _current = player;
                    _current.Tell(new GameStatusUpdate(message.Token, _gameToken, GameStatus.Created, Self, null), Self);
                }
                else
                {
                    _opponent = player;
                    _opponent.Tell(new GameStatusUpdate(_opponent.Player.Token, _gameToken, GameStatus.Created, Self, "Your opponent is " + _current.Player.Name), Self);
                    _current.Tell(new GameStatusUpdate(_current.Player.Token, _gameToken, GameStatus.PlayerJoined, Self, "Your opponent is " + _opponent.Player.Name), Self);

                    _current.CreateTable(Context, _gameToken);
                    _opponent.CreateTable(Context, _gameToken);

                    Become(WaitingForPositions);
                }
            });

            Receive<StopGame>(IsForMe, message =>
            {
                HandleStopGame(message.Token);
            });
        }

        private void WaitingForPositions()
        {
            SetReceiveTimeout(TimeSpan.FromSeconds(120));
            GameLog("Using 120 seconds receive timeout");

            Receive<GiveMeNextPosition>(message =>
            {
                var player = GetPlayer(message.Token);
                player.Tell(message, Self);
            });

            Receive<ShipPosition>(IsForMe, message =>
            {
                var player = GetPlayer(message.Token);
                player.Table.Tell(message, Self);
            });

            Receive<GameStatusUpdate>(message => message.Status == GameStatus.Configured, message =>
            {
                var player = GetPlayer(message.Token);
                player.Ready();

                var otherPlayer = GetOtherPlayer(message.Token);
                if (otherPlayer.IsInitialized)
                {
                    otherPlayer.Tell(new GameStatusUpdate(otherPlayer.Player.Token, _gameToken, GameStatus.GameStartedYouStart, Self, null), Self);
                    player.Tell(new GameStatusUpdate(player.Player.Token, _gameToken, GameStatus.GameStartedOpponentStarts, Self, null), Self);
                    if (otherPlayer.Player.Token != _current.Player.Token)
                    {
                        SwitchSides();
                    }
                    Become(GameOn);
                }
            });

            Receive<GameTable>(message =>
            {
                var player = GetPlayer(message.Token);
                player.Tell(message, Self);
            });

            Receive<StopGame>(IsForMe, message =>
            {
                HandleStopGame(message.Token);
            });

            Receive<ReceiveTimeout>(message =>
            {
                HandleStopGame(Guid.Empty, true);
            });
        }

        private void GameOn()
        {
            #region Player points

            Receive<Missile>(message => IsForMe(message) && message.Token == _current.Player.Token, message =>
            {
                _opponent.Table.Tell(message, Self);
            });

            #endregion

            #region Table responses

            Receive<GameTable>(message =>
            {
                _opponent.Tell(new GameTable(_opponent.Player.Token, _gameToken, message.Points), Self);
                _current.Tell(new GameTable(_current.Player.Token, _gameToken, RemoveShipInfo(message.Points)), Self);
            });

            Receive<MissileWasAHit>(IsForMe, message =>
            {
                string postfix = message.ShipDestroyed ? " The ship was destroyed!" : "";
                _current.Tell(new GameStatusUpdate(_current.Player.Token, _gameToken, GameStatus.ItIsYourTurn, Self, "Missile was a hit at " + message.Point + "." + postfix), Self);
                _opponent.Tell(new GameStatusUpdate(_opponent.Player.Token, _gameToken, GameStatus.None, Self, "Opponents missile was a hit at " + message.Point + "." + postfix), Self);
            });

            Receive<MissileDidNotHitShip>(IsForMe, message =>
            {
                _opponent.Tell(new GameStatusUpdate(_opponent.Player.Token, _gameToken, GameStatus.ItIsYourTurn, Self, "Opponents missile did not hit at " + message.Point), Self);
                _current.Tell(new GameStatusUpdate(_current.Player.Token, _gameToken, GameStatus.None, Self, "Your missile did not hit at " + message.Point), Self);
                SwitchSides();
            });

            Receive<GameOver>(IsForMe, message =>
            {
                Become(GameOver);
            });

            Receive<AlreadyHit>(IsForMe, message =>
            {
                var error = "Opponent used the same point again at " + message.Point;
                _opponent.Tell(new GameStatusUpdate(_opponent.Player.Token, _gameToken, GameStatus.ItIsYourTurn, Self, error), Self);
                _current.Tell(new MissileAlreadyHit(_current.Player.Token, _gameToken, message.Point), Self);
                SwitchSides();
            });

            #endregion

            Receive<StopGame>(IsForMe, message =>
            {
                HandleStopGame(message.Token);
            });

            Receive<ReceiveTimeout>(message =>
            {
                HandleStopGame(Guid.Empty, true);
            });
        }

        private void GameOver()
        {
            GameLog("Game over for " + _gameToken);
            HandleStopGame(Guid.Empty);
        }

        private void HandleStopGame(Guid userTokenWhoRequestedStopping, bool timeout = false)
        {
            if (_stopHandled)
            {
                return;
            }
            _stopHandled = true;
            if (userTokenWhoRequestedStopping != Guid.Empty)
            {
                const string message = "Game forced to stop";
                GameLog(message);
                var player = GetOtherPlayer(userTokenWhoRequestedStopping);
                player.Tell(new GameStatusUpdate(_current.Player.Token, _gameToken, GameStatus.GameOver, Self, message), Self);
            }
            else
            {
                string message = timeout ? "Game timed out" : null;
                _opponent.Tell(new GameStatusUpdate(_opponent.Player.Token, _gameToken, timeout ? GameStatus.GameOver : GameStatus.YouLost, Self, message), Self);
                _current.Tell(new GameStatusUpdate(_current.Player.Token, _gameToken, timeout ? GameStatus.GameOver : GameStatus.YouWon, Self, message), Self);
            }
            Context.Parent.Tell(new PlayersFree(_gameToken, _current.Player.Token, _opponent.Player.Token), Self);
            Context.Stop(Self);
        }

        private void SwitchSides()
        {
            var current = _current;
            _current = _opponent;
            _opponent = current;
        }

        private PlayerContainer GetPlayer(Guid token)
        {
            return _current.Player.Token == token ? _current : _opponent;
        }

        private PlayerContainer GetOtherPlayer(Guid token)
        {
            return _current.Player.Token == token ? _opponent : _current;
        }

        private bool IsForMe(GameMessage message)
        {
            if (message.GameToken == _gameToken)
            {
                return true;
            }
            GameLog("Message not for me with token " + message.GameToken);
            return false;
        }

        private void GameLog(string message)
        {
            Log.Info("Game " + _gameToken + ": " + message);
        }

        private static Point[] RemoveShipInfo(IReadOnlyList<Point> points)
        {
            var array = new Point[points.Count];
            for (var i = 0; i < points.Count; ++i)
            {
                var point = points[i];
                if (point.HasShip && !point.HasHit)
                {
                    array[i] = new Point(point.X, point.Y, false, false);
                }
                else
                {
                    array[i] = point;
                }
            }
            return array;
        }

        protected override void PostStop()
        {
            HandleStopGame(Guid.Empty);
        }

        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new AllForOneStrategy(x =>
            {
                GameLog("An exception occurred: " + x.Message);
                HandleStopGame(Guid.Empty);
                return Directive.Stop;
            });
        }
    }
}
