using Akka.Actor;
using Akka.Configuration;

namespace Actors.CSharp
{
    public static class ActorSystemContext
    {
        public static ActorSystem System { get; private set; }
        private static IActorRef GameManager { get; set; }

        public static void CreateActorSystemContext(Config config)
        {
            System = ActorSystem.Create("BattleShip", config); // TODO: config
        }

        public static void CreateManager()
        {
            GameManager = System.ActorOf(Props.Create<GameManagerActor>(), "gameManager");
        }

        public static ActorSelection VirtualManager()
        {
            return System.ActorSelection("akka.tcp://BattleShip@localhost:8080/user/gameManager");
        }
    }
}
