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

            Receive<Message.PlayersFree>(HasSender, message =>
            {
                Log.Info("Game factory to destroy the game with token " + message.GameToken);
                Context.Parent.Tell(message, Self);
                Sender.Tell(PoisonPill.Instance);
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

            Receive<Message.PlayersFree>(HasSender, message =>
            {
                Log.Info("Game factory to destroy the game with token " + message.GameToken);
                Context.Parent.Tell(message, Self);
                Sender.Tell(PoisonPill.Instance);
            });

            Receive<Message.StopGame>(message =>
            {
                Context.ActorSelection(message.Token.ToString()).Tell(message, Self);
            });
        }

        protected override SupervisorStrategy SupervisorStrategy()
        {
            return base.SupervisorStrategy(); // TODO: supervise games
        }
    }
}
