using System;
using System.Collections.Generic;
using Akka.Actor;
using Messages.CSharp;
using Messages.CSharp.Pieces;

// ReSharper disable PossibleUnintendedReferenceComparison

namespace Actors.CSharp
{
    public class GameActor : BattleShipActor
    {
        private PlayerContainer _current;
        private PlayerContainer _opponent;
        private readonly Guid _gameToken;
        private bool _stopHandled;
        private readonly ICanTell _gameManager = Context.ActorSelection("/user/gameManager");

        public GameActor(Guid gameToken)
        {
            _gameToken = gameToken;
            
            Receive<Message.PlayerJoining>(IsForMe, message =>
            {
                var player = new PlayerContainer(message.Player);
                
                GameLog("Player " + message.Player.Name + " arrived");
                if (_current == null)
                {
                    _current = player;
                    _current.Tell(new Message.GameStatusUpdate(message.Token, _gameToken, GameStatus.Created, Self), Self);
                }
                else
                {
                    _opponent = player;
                    _opponent.Tell(new Message.GameStatusUpdate(_opponent.Player.Token, _gameToken, GameStatus.Created, Self, "Your opponent is " + _current.Player.Name), Self);
                    _current.Tell(new Message.GameStatusUpdate(_current.Player.Token, _gameToken, GameStatus.PlayerJoined, Self, "Your opponent is " + _opponent.Player.Name), Self);

                    _current.CreateTable(Context, _gameToken);
                    _opponent.CreateTable(Context, _gameToken);

                    Become(WaitingForPositions);
                }
            });

            Receive<Message.StopGame>(IsForMe, message =>
            {
                HandleStopGame(message.Token);
            });
        }

        private void WaitingForPositions()
        {
            SetReceiveTimeout(TimeSpan.FromSeconds(120));
            GameLog("Using 120 seconds receive timeout");

            Receive<Message.GiveMeNextPosition>(message =>
            {
                var player = GetPlayer(message.Token);
                player.Tell(message, Self);
            });

            Receive<Message.ShipPosition>(IsForMe, message =>
            {
                var player = GetPlayer(message.Token);
                player.Table.Tell(message, Self);
            });

            Receive<Message.GameStatusUpdate>(message => message.Status == GameStatus.Configured, message =>
            {
                var player = GetPlayer(message.Token);
                player.Ready();

                var otherPlayer = GetOtherPlayer(message.Token);
                if (otherPlayer.IsInitialized)
                {
                    otherPlayer.Tell(new Message.GameStatusUpdate(otherPlayer.Player.Token, _gameToken, GameStatus.GameStartedYouStart, Self), Self);
                    player.Tell(new Message.GameStatusUpdate(player.Player.Token, _gameToken, GameStatus.GameStartedOpponentStarts, Self), Self);
                    if (otherPlayer.Player.Token != _current.Player.Token)
                    {
                        SwithSides();
                    }
                    Become(GameOn);
                }
            });

            Receive<Message.GameTable>(message =>
            {
                var player = GetPlayer(message.Token);
                player.Tell(message, Self);
            });

            Receive<Message.StopGame>(IsForMe, message =>
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

            Receive<Message.Missile>(message => IsForMe(message) && message.Token == _current.Player.Token, message => // _current.Player.Actor |> _opponent.Table
            {
                _opponent.Table.Tell(message, Self);
            });

            #endregion

            #region Table responses

            Receive<Message.GameTable>(message =>
            {
                _opponent.Tell(new Message.GameTable(_opponent.Player.Token, _gameToken, message.Points), Self);
                _current.Tell(new Message.GameTable(_current.Player.Token, _gameToken, RemoveShipInfo(message.Points)), Self);
            });

            Receive<Message.MissileWasAHit>(IsForMe, message =>
            {
                string postfix = message.ShipDestroyed ? " The ship was destroyed!" : "";
                _current.Tell(new Message.GameStatusUpdate(_current.Player.Token, _gameToken, GameStatus.ItIsYourTurn, Self, "Missile was a hit at " + message.Point + "." + postfix), Self);
                _opponent.Tell(new Message.GameStatusUpdate(_opponent.Player.Token, _gameToken, GameStatus.None, Self, "Opponents missile was a hit at " + message.Point + "." + postfix), Self);
            });

            Receive<Message.MissileDidNotHitShip>(IsForMe, message =>
            {
                _opponent.Tell(new Message.GameStatusUpdate(_opponent.Player.Token, _gameToken, GameStatus.ItIsYourTurn, Self, "Opponents missile did not hit at " + message.Point), Self);
                _current.Tell(new Message.GameStatusUpdate(_current.Player.Token, _gameToken, GameStatus.None, Self, "Your missile did not hit at " + message.Point), Self);
                SwithSides();
            });

            Receive<Message.GameOver>(IsForMe, message =>
            {
                Become(GameOver);
            });

            Receive<Message.AlreadyHit>(IsForMe, message =>
            {
                var error = "Opponent used the same point again at " + message.Point;
                _opponent.Tell(new Message.GameStatusUpdate(_opponent.Player.Token, _gameToken, GameStatus.ItIsYourTurn, Self, error), Self);
                _current.Tell(new Message.MissileAlreadyHit(_current.Player.Token, _gameToken, message.Point), Self);
                SwithSides();
            });

            #endregion

            Receive<Message.StopGame>(IsForMe, message =>
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
                player.Tell(new Message.GameStatusUpdate(_current.Player.Token, _gameToken, GameStatus.GameOver, Self, message), Self);
            }
            else
            {
                string message = timeout ? "Game timed out" : null;
                _opponent.Tell(new Message.GameStatusUpdate(_opponent.Player.Token, _gameToken, timeout ? GameStatus.GameOver : GameStatus.YouLost, Self, message), Self);
                _current.Tell(new Message.GameStatusUpdate(_current.Player.Token, _gameToken, timeout ? GameStatus.GameOver : GameStatus.YouWon, Self, message), Self);
            }
            _gameManager.Tell(new Message.PlayersFree(_gameToken, _current.Player.Token, _opponent.Player.Token), Self);
            Self.Tell(PoisonPill.Instance);
        }

        private void SwithSides()
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

        private bool IsForMe(Message.GameMessage message)
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
                if (point.HasShip)
                {
                    array[i] = new Point(point.X, point.Y, false, point.HasHit);
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
