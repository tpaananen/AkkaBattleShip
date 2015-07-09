using Actors.CSharp;
using Akka.Configuration;

namespace BattleShipServer
{
    class Program
    {
        static void Main(string[] args)
        {
            // Server
            var config = ConfigurationFactory.Load();
            ActorSystemContext.CreateActorSystemContext(config);
            using (var system = ActorSystemContext.System)
            {
                ActorSystemContext.CreateManager();
                system.AwaitTermination();
            }
        }
    }
}
