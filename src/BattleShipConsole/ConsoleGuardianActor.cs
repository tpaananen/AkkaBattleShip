using Actors.CSharp;
using Akka.Actor;
using Messages.CSharp;

namespace BattleShipConsole
{
    public class ConsoleGuardianActor : BattleShipActor
    {
        private int _counter = 0;
        private readonly Props _consoleUi;

        public ConsoleGuardianActor()
        {
            _consoleUi = Props.Create(() => new ConsoleActor());
            Receive<Message.CreatePlayer>(message =>
            {
                Context.ActorOf(Props.Create(() => new PlayerActor(message.Name, _consoleUi)), (++_counter).ToString());
            });
        }

        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(5, 1000, ex =>
            {
                Log.Error(ex.Message);
                return Directive.Restart;
            });
        }
    }
}
