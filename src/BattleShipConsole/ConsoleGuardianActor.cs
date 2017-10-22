using Actors.CSharp;
using Akka.Actor;
using Messages.FSharp;

namespace BattleShipConsole
{
    public class ConsoleGuardianActor : BattleShipActor
    {
        private int _counter;
        private readonly Props _consoleUi;
        private readonly IActorRef _reader;

        public ConsoleGuardianActor()
        {
            _reader = Context.ActorOf(Props.Create(() => new ConsoleReaderActor()), "reader");
            _consoleUi = Props.Create(() => new ConsoleActor());

            Receive<CreatePlayer>(message =>
            {
                Context.ActorOf(Props.Create(() => new PlayerActor(message.Name, _consoleUi, _reader)), (++_counter).ToString());
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
