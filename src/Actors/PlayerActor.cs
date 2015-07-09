using System;
using Akka.Actor;
using Messages.CSharp;
using Messages.CSharp.Pieces;

// ReSharper disable PossibleUnintendedReferenceComparison

namespace Actors.CSharp
{
    public class PlayerActor : BattleShipActor
    {
        private Guid _token;
        private readonly IActorRef _playerUserInterface;

        private Guid _currentGameToken;
        private IActorRef _currentGame;

        private readonly string _name;

        public PlayerActor(string name, Props propsForUserInteracting)
        {
            _name = name;
            _playerUserInterface = Context.ActorOf(propsForUserInteracting, "ui");
            Become(Unregistered);
        }

        protected override void PreStart()
        {
            Self.Tell("register");
            base.PreStart();
        }

        private void Unregistered()
        {
            Receive<string>(message => message == "register", message =>
            {
                ActorSystemContext.VirtualManager().Tell(new MessageRegisterPlayer(_name), Self);
            });

            Receive<MessageRegisterPlayerResponse>(message =>
            {
                if (message.IsValid)
                {
                    _token = message.Token;
                    Become(InLobby);
                }
                else
                {
                    _playerUserInterface.Tell(message.Errors, Self);
                }
            });
        }

        private void InLobby()
        {
            _currentGameToken = Guid.Empty;
            _currentGame = null;
            
            Receive<string>(message => message == "join", message =>
            {
                ActorSystemContext.VirtualManager().Tell(new MessageCreateGame(_token), Self);
            });

            Receive<MessageGameStatusUpdate>(message => message.Status == GameStatus.Created || message.Status == GameStatus.PlayerJoined, message =>
            {
                _currentGameToken = message.GameToken;
                _currentGame = message.Game;
            });

            Receive<MessageGiveMeYourPositions>(message =>
            {
                if (message.GameToken == _currentGameToken)
                {
                    _playerUserInterface.Tell(message, Self);
                }
            });

            Receive<MessagePlayerPositions>(message =>
            {
                _currentGame.Tell(new MessagePlayerPositions(_token, _currentGameToken, message.Ships), Self);
                Become(WaitingForGameStart);
            });

            Receive<string>(message => message == "unregister", message =>
            {
                ActorSystemContext.VirtualManager().Tell(new MessageUnregisterPlayer(_token));
                ActorSystemContext.System.Shutdown(); // stopping the client
            });

            ReceiveAny(message =>
            {
                Log.Debug("Unhandled message of type " + message.GetType() + " received in InLobby state...");
            });

            _playerUserInterface.Tell("join", Self);
        }

        private void WaitingForGameStart()
        {
            Receive<MessageGameStatusUpdate>(message =>
            {
                if (message.GameToken != _currentGameToken)
                {
                    Log.Debug("Invalid game token in MessageGameStatusUpdate");
                    return;
                }

                if (message.Status == GameStatus.GameStartedOpponentStarts)
                {
                    Become(GameStarted);
                }
                else if (message.Status == GameStatus.GameStartedYouStart)
                {
                    _playerUserInterface.Tell("coord", Self);
                    Become(GameStarted);
                }
                else
                {
                    _currentGame = null;
                    _currentGameToken = Guid.Empty;
                    Become(InLobby);
                }
            });

            ReceiveAny(message =>
            {
                Log.Debug("Unhandled message of type " + message.GetType() + " received in WaitingForGameStart state...");
            });
        }

        private void GameStarted()
        {
            Receive<MessageTable>(IsForMe, message =>
            {
                _playerUserInterface.Tell(message, Self);
            });

            Receive<MessageGameStatusUpdate>(message => IsForMe(message) && message.Status == GameStatus.ItIsYourTurn, message =>
            {
                if (!string.IsNullOrEmpty(message.Message))
                {
                    _playerUserInterface.Tell(message.Message);
                }
                _playerUserInterface.Tell("coord", Self);
            });

            Receive<MessageGameStatusUpdate>(message => IsForMe(message) && message.Status == GameStatus.None && !string.IsNullOrEmpty(message.Message), message =>
            {
                _playerUserInterface.Tell(message.Message, Self);
            });

            Receive<MessageAlreadyHit>(IsForMe, message =>
            {
                _playerUserInterface.Tell("The point was already used, sorry!", Self);
            });

            Receive<Point>(point =>
            {
                _currentGame.Tell(new MessageMissile(_token, _currentGameToken, point), Self);
            });

            Receive<MessageGameStatusUpdate>(message => message.Token == _token && message.Status == GameStatus.YouWon || message.Status == GameStatus.YouLost, message =>
            {
                if (message.Status == GameStatus.YouWon)
                {
                    _playerUserInterface.Tell("You won!", Self);
                }
                else
                {
                    _playerUserInterface.Tell("You lost!", Self);
                }
                // Do something
                _currentGameToken = Guid.Empty;
                _currentGame = null;
                Become(InLobby);
            });

            ReceiveAny(message =>
            {
                Log.Debug("Unhandled message of type " + message.GetType() + " received in GameStarted state...");
            });
        }

        private bool IsForMe(GameMessageWithToken message)
        {
            return message.Token == _token && message.GameToken == _currentGameToken;
        }

        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(10, 10, ex =>
            {
                Log.Error(ex.ToString());
                if (ex is FormatException)
                {
                    return Directive.Resume;
                }
                return Directive.Restart;
            });
        }
    }
}
