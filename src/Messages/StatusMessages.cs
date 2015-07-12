using System;
using Akka.Actor;
using Messages.CSharp.Pieces;

namespace Messages.CSharp
{
    public partial class Message
    {

        public abstract class WithPoint : GameMessage
        {
            public Point Point { get; private set; }

            protected WithPoint(Guid token, Guid gameToken, Point point)
                : base(token, gameToken)
            {
                Point = point;
            }
        }

        public class ShipDestroyed : WithPoint
        {
            public ShipDestroyed(Guid token, Guid gameToken, Point point) : base(token, gameToken, point)
            {
            }
        }

        public class MissileDidNotHitShip : WithPoint
        {
            public MissileDidNotHitShip(Guid token, Guid gameToken, Point point) : base(token, gameToken, point)
            {
            }
        }

        public class AlreadyHit : WithPoint
        {
            public AlreadyHit(Guid token, Guid gameToken, Point point) : base(token, gameToken, point)
            {
            }
        }

        public class PartOfTheShipDestroyed : WithPoint
        {
            public PartOfTheShipDestroyed(Guid token, Guid gameToken, Point point)
                : base(token, gameToken, point)
            {
            }
        }

        public class GameOver : GameMessage
        {
            public GameOver(Guid token, Guid gameToken) : base(token, gameToken)
            {
            }
        }

        public class PlayersFree
        {
            public Guid GameToken { get; private set; }

            public Guid[] Tokens { get; private set; }

            public PlayersFree(Guid gameToken, params Guid[] tokens)
            {
                GameToken = gameToken;
                Tokens = tokens;
            }
        }
    }
}
