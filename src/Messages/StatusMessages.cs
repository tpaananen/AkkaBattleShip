using System;
using Akka.Actor;
using Messages.CSharp.Pieces;

namespace Messages.CSharp
{
    public class MessageYouArePartOfShip
    {
        public IActorRef ShipActorRef { get; private set; }

        public MessageYouArePartOfShip(IActorRef actorRef)
        {
            ShipActorRef = actorRef;
        }
    }

    public abstract class MessageWithPoint : GameMessageWithToken
    {
        public Point Point { get; private set; }

        protected MessageWithPoint(Guid token, Guid gameToken, Point point)
            : base(token, gameToken)
        {
            Point = point;
        }
    }

    public class MessageShipDestroyed : MessageWithPoint
    {
        public MessageShipDestroyed(Guid token, Guid gameToken, Point point) : base(token, gameToken, point) { }
    }

    public class MessageMissileDidNotHitShip : MessageWithPoint
    {
        public MessageMissileDidNotHitShip(Guid token, Guid gameToken, Point point) : base(token, gameToken, point)
        {
        }
    }

    public class MessageAlreadyHit : MessageWithPoint
    {
        public MessageAlreadyHit(Guid token, Guid gameToken, Point point) : base(token, gameToken, point)
        {
        }
    }

    public class MessagePartOfTheShipDestroyed : MessageWithPoint
    {
        public MessagePartOfTheShipDestroyed(Guid token, Guid gameToken, Point point) : base(token, gameToken, point)
        {
        }
    }

    public class MessageGameOver : GameMessageWithToken
    {
        public MessageGameOver(Guid token, Guid gameToken) : base(token, gameToken)
        {
        }
    }

    public class MessagePlayersFree
    {
        public Guid GameToken { get; private set; }

        public Guid[] Tokens { get; private set; }

        public MessagePlayersFree(Guid gameToken, params Guid[] tokens)
        {
            GameToken = gameToken;
            Tokens = tokens;
        }
    }
}
