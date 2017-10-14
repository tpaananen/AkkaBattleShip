using System;
using Actors.CSharp;
using Akka.Actor;
using Akka.TestKit.NUnit;
using Messages.FSharp;
using NUnit.Framework;

namespace BattleShipTests
{
    [TestFixture]
    public class GameManagerActorTests : TestKit
    {
        [Test]
        public void RegisterPlayer()
        {
            var manager = Sys.ActorOf(Props.Create<GameManagerActor>());
            manager.Tell(new RegisterPlayer("MyName"));
            ExpectMsg<RegisterPlayerResponse>(TimeSpan.FromSeconds(1));
        }
    }
}
