﻿using System;
using System.Collections.Generic;
using Akka.Actor;
using Messages.CSharp;
using Messages.CSharp.Containers;
using Messages.CSharp.Pieces;

// ReSharper disable PossibleUnintendedReferenceComparison

namespace Actors.CSharp
{
    public class GameActor : BattleShipActor
    {
        private class PlayerContainer
        {
            public ActorInfoContainer Player { get; private set; }
            public IActorRef Table { get; private set; }
            public bool IsInitialized { get; private set; }

            public PlayerContainer(ActorInfoContainer player)
            {
                Player = player;
                Table = null;
                IsInitialized = false;
            }

            public void CreateTable(Guid gameToken, IReadOnlyList<Ship> ships)
            {
                Table = Context.ActorOf(Props.Create(() => new GameTableActor(gameToken, ships)), "P1:" + Player.Name);
                IsInitialized = true;
            }

            public void Tell(object message, IActorRef sender)
            {
                Player.Actor.Tell(message, sender);
            }
        }

        private PlayerContainer _current;
        private PlayerContainer _opponent;
        private readonly Guid _gameToken;

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

                    _current.Tell(new Message.GiveMeYourPositions(_current.Player.Token, _gameToken, TablesAndShips.Ships), Self);
                    _opponent.Tell(new Message.GiveMeYourPositions(_opponent.Player.Token, _gameToken, TablesAndShips.Ships), Self);
                    Become(WaitingForPositions);
                }
            });

            Receive<Message.StopGame>(IsForMe, message =>
            {
                HandleStopGame(message.Token);
            });

            ReceiveAny(message =>
            {
                GameLog("Unhandled message of type " + message.GetType() + " received in intial state...");
            });
        }

        private void WaitingForPositions()
        {
            SetReceiveTimeout(TimeSpan.FromSeconds(120));
            GameLog("Using 120 seconds receive timeout");

            Receive<Message.ShipPositions>(IsForMe, message =>
            {
                var player = GetPlayer(message.Token);
                var otherPlayer = GetOtherPlayer(message.Token);

                GameLog("Player " + player.Player.Name + " ships have arrived");

                player.CreateTable(_gameToken, message.Ships);
                if (player.IsInitialized && otherPlayer.IsInitialized)
                {
                    // First player that provided the ships will start the game
                    otherPlayer.Tell(new Message.GameStatusUpdate(otherPlayer.Player.Token, _gameToken, GameStatus.GameStartedYouStart, Self), Self);
                    player.Tell(new Message.GameStatusUpdate(player.Player.Token, _gameToken, GameStatus.GameStartedOpponentStarts, Self), Self);
                    if (otherPlayer.Player.Token != _current.Player.Token)
                    {
                        SwithSides();
                    }
                    Become(GameOn);
                }
            });

            Receive<Message.StopGame>(IsForMe, message =>
            {
                HandleStopGame(message.Token);
            });

            Receive<ReceiveTimeout>(message =>
            {
                HandleStopGame(Guid.Empty, true);
            });

            ReceiveAny(message =>
            {
                GameLog("Unhandled message of type " + message.GetType() + " received in WaitingForPositions state...");
            });
        }

        // TODO: table to current user where there is no ship info

        private void GameOn()
        {
            #region Player points

            Receive<Message.Missile>(message => IsForMe(message) && message.Token == _current.Player.Token, message => // _current.Player.Actor |> _opponent.Table
            {
                _opponent.Table.Tell(message, Self);
            });

            #endregion

            #region Table responses

            Receive<IReadOnlyList<Point>>(message =>
            {
                _opponent.Tell(new Message.GameTable(_opponent.Player.Token, _gameToken, message), Self);
                _current.Tell(new Message.GameTable(_current.Player.Token, _gameToken, RemoveShipInfo(message)), Self);
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

            ReceiveAny(message =>
            {
                GameLog("Unhandled message of type " + message.GetType() + " received in PlayerOne state...");
            });
        }

        private void GameOver()
        {
            GameLog("Game over for " + _gameToken);
            HandleStopGame(Guid.Empty);
        }

        private void HandleStopGame(Guid userTokenWhoRequestedStopping, bool timeout = false)
        {
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
            Context.Parent.Tell(new Message.PlayersFree(_gameToken, _current.Player.Token, _opponent.Player.Token), Self);
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
    }
}
