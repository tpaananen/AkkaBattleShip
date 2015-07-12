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

        public static ActorSelection VirtualManager()
        {
            return System.ActorSelection("akka.tcp://BattleShip@127.0.0.1:8080/user/gameManager");
        }
    }
}
