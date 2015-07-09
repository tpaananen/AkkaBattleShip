using System;
using System.Collections.Generic;
using Akka.Actor;
using Messages.CSharp.Containers;
using Messages.CSharp.Pieces;

namespace Messages.CSharp
{
    public class MessageCreateGame : MessageWithToken
    {
        public MessageCreateGame(Guid token)
            : base(token)
        {
        }
    }

    public class MessageUnableToCreateGame : MessageWithToken
    {
        public string Error { get; private set; }

        public MessageUnableToCreateGame(Guid token, string error) : base(token)
        {
            Error = error;
        }
    }

    public class MessagePlayerArrived
    {
        public ActorInfoContainer Player { get; private set; }

        public MessagePlayerArrived(ActorInfoContainer player)
        {
            Player = player;
        }
    }

    public enum GameStatus
    {
        None = 0,
        Created = 1,
        PlayerJoined = 2,
        GameStartedYouStart = 3,
        GameStartedOpponentStarts = 4,
        ItIsYourTurn = 5,
        YouWon = 6,
        YouLost = 7,
        GameOver = 8
    }

    public class MessageGameStatusUpdate : GameMessageWithToken
    {
        public IActorRef Game { get; private set; }

        public GameStatus Status { get; private set; }

        public string Message { get; private set; }

        public MessageGameStatusUpdate(Guid token, Guid gameToken, GameStatus status, IActorRef game, string message = null) : base(token, gameToken)
        {
            Status = status;
            Game = game;
            Message = message;
        }
    }

    public class MessagePlayerJoining : GameMessageWithToken
    {
        public ActorInfoContainer Player { get; private set; }

        public MessagePlayerJoining(Guid gameToken, ActorInfoContainer player)
            : base(player.Token, gameToken)
        {
            Player = player;
        }
    }

    public abstract class GameMessageWithToken : MessageWithToken
    {
        public Guid GameToken { get; private set; }

        protected GameMessageWithToken(Guid token, Guid gameToken) : base(token)
        {
            GameToken = gameToken;
        }
    }

    public class MessagePlayerPositions : GameMessageWithToken
    {
        public IReadOnlyList<Ship> Ships { get; private set; }

        public MessagePlayerPositions(Guid token, Guid gameToken, IReadOnlyList<Ship> ships)
            : base(token, gameToken)
        {
            Ships = ships;
        }
    }

    public class MessageGiveMeYourPositions : GameMessageWithToken
    {
        public MessageGiveMeYourPositions(Guid token, Guid gameToken) : base(token, gameToken)
        {
        }
    }

    public class MessageMissile : GameMessageWithToken
    {
        public Point Point { get; private set; }

        public MessageMissile(Guid token, Guid gameToken, Point point)
            : base(token, gameToken)
        {
            Point = point;
        }
    }

    public class MessageTable : GameMessageWithToken
    {
        public Point[] Points { get; private set; }

        public MessageTable(Guid token, Guid gameToken, Point[] points)
            : base(token, gameToken)
        {
            Points = points;
        }
    }

    public class MessageMissileAlreadyHit : GameMessageWithToken
    {
        public Point Point { get; private set; }

        public MessageMissileAlreadyHit(Guid token, Guid gameToken, Point point) 
            : base(token, gameToken)
        {
            Point = point;
        }
    }

    public class MessageMissileWasAHit : MessageWithPoint
    {
        public bool ShipDestroyed { get; private set; }

        public MessageMissileWasAHit(Guid token, Guid gameToken, Point point, bool shipDestroyed = false) : base(token, gameToken, point)
        {
            ShipDestroyed = shipDestroyed;
        }

    }
}
