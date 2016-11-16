using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Actors.CSharp;
using Akka.Actor;
using Akka.Configuration;
using Akka.TestKit;
using Akka.TestKit.NUnit;
using Messages.CSharp;
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
            manager.Tell(new Message.RegisterPlayer("MyName"));
            ExpectMsg<Message.RegisterPlayerResponse>(TimeSpan.FromSeconds(1));
        }
    }
}
