using System;
using Akka.Actor;

namespace Messages.CSharp.Containers
{
    public struct ActorInfoContainer : IEquatable<ActorInfoContainer>
    {
        public readonly string Name;
        public readonly Guid Token;
        public readonly IActorRef Actor;

        public ActorInfoContainer(string name, IActorRef actorRef, Guid token)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException("name");
            }
            if (actorRef == null)
            {
                throw new ArgumentNullException("actorRef");
            }
            Name = name;
            Token = token;
            Actor = actorRef;
        }

        public bool Equals(ActorInfoContainer other)
        {
            return string.Equals(Actor, other.Actor);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is ActorInfoContainer && Equals((ActorInfoContainer)obj);
        }

        public override int GetHashCode()
        {
            return Actor.GetHashCode();
        }

        public static bool operator ==(ActorInfoContainer left, ActorInfoContainer right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ActorInfoContainer left, ActorInfoContainer right)
        {
            return !left.Equals(right);
        }
    }
}
