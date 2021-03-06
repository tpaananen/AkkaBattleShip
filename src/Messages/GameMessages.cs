﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using Akka.Actor;
using Messages.CSharp.Containers;
using Messages.CSharp.Pieces;

namespace Messages.CSharp
{
    [DebuggerStepThrough]
    public abstract partial class Message
    {

        public class CreateGame : WithToken
        {
            public CreateGame(Guid token)
                : base(token)
            {
            }
        }

        public class UnableToCreateGame : WithToken
        {
            public string Error { get; private set; }

            public UnableToCreateGame(Guid token, string error) : base(token)
            {
                Error = error;
            }
        }

        public class PlayerArrived
        {
            public ActorInfoContainer Player { get; private set; }

            public PlayerArrived(ActorInfoContainer player)
            {
                Player = player;
            }
        }

        public class GameStatusUpdate : GameMessage
        {
            public IActorRef Game { get; private set; }

            public GameStatus Status { get; private set; }

            public string Message { get; private set; }

            public GameStatusUpdate(Guid token, Guid gameToken, GameStatus status, IActorRef game,
                string message = null) : base(token, gameToken)
            {
                Status = status;
                Game = game;
                Message = message;
            }
        }

        public class PlayerJoining : GameMessage
        {
            public ActorInfoContainer Player { get; private set; }

            public PlayerJoining(Guid gameToken, ActorInfoContainer player)
                : base(player.Token, gameToken)
            {
                Player = player;
            }
        }

        public abstract class GameMessage : WithToken
        {
            public Guid GameToken { get; private set; }

            protected GameMessage(Guid token, Guid gameToken) : base(token)
            {
                GameToken = gameToken;
            }
        }

        public class ShipPosition : GameMessage
        {
            public Ship Ship { get; private set; }

            public ShipPosition(Guid token, Guid gameToken, Ship ship)
                : base(token, gameToken)
            {
                Ship = ship;
            }
        }

        public class GiveMeNextPosition : GameMessage
        {
            public Tuple<string, int> Config { get; private set; }

            public string ErrorInPreviousConfig { get; private set; }

            public GiveMeNextPosition(Guid token, Guid gameToken, Tuple<string, int> config, string error)
                : base(token, gameToken)
            {
                Config = config;
                ErrorInPreviousConfig = error;
            }
        }

        public class Missile : GameMessage
        {
            public Point Point { get; private set; }

            public Missile(Guid token, Guid gameToken, Point point)
                : base(token, gameToken)
            {
                Point = point;
            }
        }

        public class GameTable : GameMessage
        {
            public IReadOnlyList<Point> Points { get; private set; }

            public GameTable(Guid token, Guid gameToken, IReadOnlyList<Point> points)
                : base(token, gameToken)
            {
                Points = points;
            }
        }

        public class MissileAlreadyHit : GameMessage
        {
            public Point Point { get; private set; }

            public MissileAlreadyHit(Guid token, Guid gameToken, Point point)
                : base(token, gameToken)
            {
                Point = point;
            }
        }

        public class MissileWasAHit : WithPoint
        {
            public bool ShipDestroyed { get; private set; }

            public MissileWasAHit(Guid token, Guid gameToken, Point point, bool shipDestroyed = false)
                : base(token, gameToken, point)
            {
                ShipDestroyed = shipDestroyed;
            }

        }

        public class StopGame : GameMessage
        {
            public StopGame(Guid userToken, Guid gameToken) : base(userToken, gameToken)
            {
            }
        }
    }
}
