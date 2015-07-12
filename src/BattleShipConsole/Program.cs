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
            Console.WriteLine("Give me ya name: ");
            var name = Console.ReadLine();

            // Client
            var config = ConfigurationFactory.Load();
            using (var system = ActorSystemContext.CreateActorSystemContext(config))
            {
                var consoleGuardian = system.ActorOf(Props.Create(() => new ConsoleGuardianActor()), "playerguardian");
                consoleGuardian.Tell(new Message.CreatePlayer(name));
                system.AwaitTermination();
            }
        }
    }
}
