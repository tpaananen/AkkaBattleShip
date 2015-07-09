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

    public abstract class MessageWithPoint
    {
        public Point Point { get; private set; }

        protected MessageWithPoint(Point point)
        {
            Point = point;
        }
    }

    public class MessageShipDestroyed
    {
    }

    public class MessageMissileDidNotHitShip : MessageWithPoint
    {
        public MessageMissileDidNotHitShip(Point point) : base(point)
        {
        }
    }

    public class MessageAlreadyHit : MessageWithPoint
    {
        public MessageAlreadyHit(Point point) : base(point)
        {
        }
    }

    public class MessagePartOfTheShipDestroyed : MessageWithPoint
    {
        public MessagePartOfTheShipDestroyed(Point point) : base(point)
        {
        }
    }

    public class MessageGameOver
    {
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
