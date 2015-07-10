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

            Receive<MessagePlayerArrived>(message =>
            {
                _currentGameToken = Guid.NewGuid();
                Log.Info("The first player arrived, forwarding to game with token " + _currentGameToken);

                _gameUnderConstruction = Context.ActorOf(Props.Create(() => new GameActor(_currentGameToken)), _currentGameToken.ToString());
                _gameUnderConstruction.Tell(new MessagePlayerJoining(_currentGameToken, message.Player), Self);
                Become(WaitingForSecondPlayer);
            });

            Receive<MessageGameStatusUpdate>(message => HasSender(message) && message.Status == GameStatus.GameOver, message =>
            {
                Log.Info("Game factory to destroy the game with token " + message.GameToken);
                Sender.Tell(PoisonPill.Instance);
            });
        }

        private void WaitingForSecondPlayer()
        {
            Receive<MessagePlayerArrived>(message =>
            {
                Log.Info("The second player arrived, forwarding to game with token " + _currentGameToken);
                _gameUnderConstruction.Tell(new MessagePlayerJoining(_currentGameToken, message.Player), Self);
                Context.Parent.Tell(new MessageGameStatusUpdate(message.Player.Token, _currentGameToken, GameStatus.Created, _gameUnderConstruction), Self);
                Become(WaitingForFirstPlayer);
            });

            Receive<MessageGameStatusUpdate>(message => HasSender(message) && message.Status == GameStatus.GameOver, message =>
            {
                Log.Info("Game factory to destroy the game with token " + message.GameToken);
                Sender.Tell(PoisonPill.Instance);
            });
        }

        protected override SupervisorStrategy SupervisorStrategy()
        {
            return base.SupervisorStrategy(); // TODO: supervise games
        }
    }
}
