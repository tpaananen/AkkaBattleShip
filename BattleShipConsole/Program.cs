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

            var port = new Random().Next(30000, 65000);

            var config = ConfigurationFactory.ParseString(@"
            akka {
                actor {
                    provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                }
                remote {
                    helios.tcp {
                        port = " + port + @"
                        hostname = localhost
                    }
                }
            }");

            ActorSystemContext.CreateActorSystemContext(config);
            using (var system = ActorSystemContext.System)
            {
                var consoleGuardian = system.ActorOf(Props.Create(() => new ConsoleGuardianActor()), "playerguardian");

                Console.WriteLine("Give me ya name: ");
                var name = Console.ReadLine();

                var props = Props.Create(() => new ConsoleActor());
                var playerProps = Props.Create(() => new PlayerActor(name, props));
                consoleGuardian.Tell(new MessageCreatePlayer(playerProps));
                system.AwaitTermination();
            }
        }
    }
}
