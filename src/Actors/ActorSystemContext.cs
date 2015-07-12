using Akka.Actor;
using Akka.Configuration;

namespace Actors.CSharp
{
    public static class ActorSystemContext
    {
        public static ActorSystem System { get; private set; }
        private static IActorRef GameManager { get; set; }

        public static ActorSystem CreateActorSystemContext(Config config)
        {
            return (System = ActorSystem.Create("BattleShip", config));
        }

        public static void CreateManager()
        {
            GameManager = System.ActorOf(Props.Create<GameManagerActor>(), "gameManager");
        }

        /// <summary>
        /// Retrieves game manager path from the config and returns an actor selection.
        /// </summary>
        /// <returns></returns>
        public static ActorSelection VirtualManager()
        {
            var path = CreateActorPathFromConfig("/user/gameManager");
            return System.ActorSelection(path);
        }

        private static ActorPath CreateActorPathFromConfig(string path)
        {
            var remotePath = ConfigurationFactory.Load()
                    .GetString("akka.remote.deployment." + path + ".remote");
            return ActorPath.Parse(remotePath + path);
        }
    }
}
