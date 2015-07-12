using Actors.CSharp;
using Akka.Actor;
using Messages.CSharp;

namespace BattleShipConsole
{
    public class ConsoleGuardianActor : BattleShipActor
    {
        private int _counter = 0;

        public ConsoleGuardianActor()
        {
            Receive<Message.CreatePlayer>(message =>
            {
                Context.ActorOf(message.Props, (++_counter).ToString());
            });
        }

        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(5, 1000, ex =>
            {
                Log.Error(ex.ToString());
                return Directive.Restart;
            });
        }
    }
}
