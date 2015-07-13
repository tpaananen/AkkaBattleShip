using System;
using Akka.Actor;
using Messages.CSharp;

namespace Actors.CSharp
{
    public class GameFactoryActor : BattleShipActor
    {
        private IActorRef _gameUnderConstruction;
        private Guid _currentGameToken;

        public GameFactoryActor()
        {
            Become(WaitingForFirstPlayer);
        }

        private void WaitingForFirstPlayer()
        {
            _gameUnderConstruction = null;
            _currentGameToken = Guid.Empty;

            Receive<Message.PlayerArrived>(message =>
            {
                _currentGameToken = Guid.NewGuid();
                Log.Info("The first player arrived, forwarding to game with token " + _currentGameToken);

                _gameUnderConstruction = Context.ActorOf(Props.Create(() => new GameActor(_currentGameToken)), _currentGameToken.ToString());
                _gameUnderConstruction.Tell(new Message.PlayerJoining(_currentGameToken, message.Player), Self);
                Become(WaitingForSecondPlayer);
            });

            Receive<Message.StopGame>(message =>
            {
                StopGame(message);
            });
        }

        private void WaitingForSecondPlayer()
        {
            Receive<Message.PlayerArrived>(message =>
            {
                Log.Info("The second player arrived, forwarding to game with token " + _currentGameToken);
                _gameUnderConstruction.Tell(new Message.PlayerJoining(_currentGameToken, message.Player), Self);
                Become(WaitingForFirstPlayer);
            });

            Receive<Message.StopGame>(message =>
            {
                StopGame(message);
            });
        }

        protected override void PreRestart(Exception reason, object message)
        {
            if (_currentGameToken != Guid.Empty)
            {
                _gameUnderConstruction.Tell(new Message.StopGame(Guid.Empty, _currentGameToken));
            }
            base.PreRestart(reason, message);
        }

        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(x => Directive.Resume);
        }

        private void StopGame(Message.StopGame message)
        {
            Context.ActorSelection(message.GameToken.ToString()).Tell(message, Self);
        }
    }
}
