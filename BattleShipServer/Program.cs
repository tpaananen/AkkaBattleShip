using Actors.CSharp;
using Akka.Configuration;

namespace BattleShipServer
{
    class Program
    {
        static void Main(string[] args)
        {
            // Server

            var config = ConfigurationFactory.ParseString(@"
            akka {
                actor {
                    provider = ""Akka.Remote.RemoteActorRefProvider, Akka.Remote""
                }

                remote {
                    helios.tcp {
                        port = 8080
                        hostname = localhost
                    }
                }
            }");

            ActorSystemContext.CreateActorSystemContext(config);
            using (var system = ActorSystemContext.System)
            {
                ActorSystemContext.CreateManager();
                system.AwaitTermination();
            }
        }
    }
}
