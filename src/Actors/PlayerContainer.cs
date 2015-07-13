using System;
using Akka.Actor;
using Messages.CSharp.Containers;

namespace Actors.CSharp
{
    class PlayerContainer
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

        public void CreateTable(IActorContext context, Guid gameToken)
        {
            Table = context.ActorOf(Props.Create(() => new GameTableActor(Player.Token, gameToken)), Player.Token.ToString());
        }

        public void Ready()
        {
            IsInitialized = true;
        }

        public void Tell(object message, IActorRef sender)
        {
            Player.Actor.Tell(message, sender);
        }
    }
}
