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
            using (var system = ActorSystemContext.CreateActorSystemContext(config))
            {
                ActorSystemContext.CreateManager();
                system.AwaitTermination();
            }
        }
    }
}
