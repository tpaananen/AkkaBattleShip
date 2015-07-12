using System;
using Actors.CSharp;
using Akka.Actor;
using Akka.Configuration;
using Messages.CSharp;

namespace BattleShipConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            // Client
            var config = ConfigurationFactory.Load();
            using (var system = ActorSystemContext.CreateActorSystemContext(config))
            {
                var consoleGuardian = system.ActorOf(Props.Create(() => new ConsoleGuardianActor()), "playerguardian");

                Console.WriteLine("Give me ya name: ");
                var name = Console.ReadLine();

                var props = Props.Create(() => new ConsoleActor());
                var playerProps = Props.Create(() => new PlayerActor(name, props));
                consoleGuardian.Tell(new Message.CreatePlayer(playerProps));
                system.AwaitTermination();
            }
        }
    }
}
