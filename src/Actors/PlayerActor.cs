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

        private readonly ICanTell _gameManager;
        private readonly IActorRef _reader;

        public PlayerActor(string name, Props propsForUserInteracting, IActorRef reader)
        {
            _reader = reader;
            _name = name;
            _playerUserInterface = Context.ActorOf(propsForUserInteracting, "ui");
            _reader.Tell(_playerUserInterface);
            _gameManager = ActorSystemContext.VirtualManager();
            Become(Unregistered);
        }

        protected override void PreRestart(Exception reason, object message)
        {
            _reader.Tell(_playerUserInterface);
            if (_token != Guid.Empty)
            {
                _gameManager.Tell(new Message.UnregisterPlayer(_token, _currentGameToken), Self);
            }
            Context.Stop(_playerUserInterface);
            base.PreRestart(reason, message);
        }

        protected override void PreStart()
        {
            Self.Tell("register");
            base.PreStart();
        }

        private void Unregistered()
        {
            _currentGameToken = Guid.Empty;
            _currentGame = null;

            Receive<string>(message => message == "register", message =>
            {
                _gameManager.Tell(new Message.RegisterPlayer(_name), Self);
            });

            Receive<Message.RegisterPlayerResponse>(message =>
            {
                if (message.IsValid)
                {
                    _token = message.Token;
                    _playerUserInterface.Tell("Registered to server with token " + message.Token, Self);
                    Become(InLobby);
                }
                else
                {
                    _playerUserInterface.Tell(message.Errors, Self);
                }
            });

            Receive<Message.UnableToCreateGame>(message =>
            {
                _playerUserInterface.Tell("Unable to create game, " + message.Error, Self);
                _playerUserInterface.Tell("join", Self);
            });
        }

        private void InLobby()
        {
            _currentGameToken = Guid.Empty;
            _currentGame = null;
            Receive<string>(message => message == "join", message =>
            {
                _gameManager.Tell(new Message.CreateGame(_token), Self);
            });

            Receive<Message.GameStatusUpdate>(message =>
            {
                switch (message.Status)
                {
                    case GameStatus.Created:
                        _currentGameToken = message.GameToken;
                        _currentGame = message.Game;
                        if (!string.IsNullOrEmpty(message.Message))
                        {
                            _playerUserInterface.Tell(message.Message, Self);
                        }
                        else
                        {
                            _playerUserInterface.Tell("Game created with token " + message.GameToken, Self);
                        }
                        break;
                    case GameStatus.PlayerJoined:
                        _playerUserInterface.Tell(message.Message, Self);
                        break;
                    case GameStatus.GameStartedOpponentStarts:
                        _playerUserInterface.Tell("Opponent starts...");
                        Become(GameStarted);
                        break;
                    case GameStatus.GameStartedYouStart:
                        _playerUserInterface.Tell("coord", Self);
                        Become(GameStarted);
                        break;
                    default:
                        _currentGame = null;
                        _currentGameToken = Guid.Empty;
                        if (!string.IsNullOrEmpty(message.Message))
                        {
                            _playerUserInterface.Tell(message, Self);
                        }
                        Become(InLobby);
                        break;
                }
            });

            Receive<Message.GiveMeNextPosition>(message =>
            {
                if (message.GameToken == _currentGameToken)
                {
                    _playerUserInterface.Tell(message, Self);
                }
            });

            Receive<Message.ShipPosition>(message =>
            {
                if (_currentGame != null)
                {
                    _currentGame.Tell(new Message.ShipPosition(_token, _currentGameToken, message.Ship), Self);
                }
            });

            Receive<Message.GameTable>(IsForMe, message =>
            {
                _playerUserInterface.Tell(message, Self);
            });

            Receive<string>(message => message == "unregister", async message =>
            {
                _gameManager.Tell(new Message.UnregisterPlayer(_token, _currentGameToken), Self);
                await ActorSystemContext.System.Terminate().ConfigureAwait(false); // stopping the client
            });

            ReceiveAny(message =>
            {
                Log.Debug("Unhandled message of type " + message.GetType() + " received in InLobby state...");
            });

            _playerUserInterface.Tell("join", Self);
        }

        private void GameStarted()
        {
            Receive<Message.GameTable>(IsForMe, message =>
            {
                _playerUserInterface.Tell(message, Self);
            });

            Receive<Message.GameStatusUpdate>(message => IsForMe(message) && message.Status == GameStatus.ItIsYourTurn, message =>
            {
                if (!string.IsNullOrEmpty(message.Message))
                {
                    _playerUserInterface.Tell(message.Message);
                }
                _playerUserInterface.Tell("coord", Self);
            });

            Receive<Message.GameStatusUpdate>(message => IsForMe(message) && message.Status == GameStatus.None && !string.IsNullOrEmpty(message.Message), message =>
            {
                _playerUserInterface.Tell(message.Message, Self);
            });

            Receive<Message.MissileAlreadyHit>(IsForMe, message =>
            {
                _playerUserInterface.Tell("The point was already used, sorry!", Self);
            });

            Receive<Point>(point =>
            {
                _currentGame.Tell(new Message.Missile(_token, _currentGameToken, point), Self);
            });

            Receive<Message.GameStatusUpdate>(message => IsForMe(message) && 
                (message.Status == GameStatus.YouWon || 
                 message.Status == GameStatus.YouLost || 
                 message.Status == GameStatus.GameOver), 
            message =>
            {
                _playerUserInterface.Tell(message, Self);
                _currentGameToken = Guid.Empty;
                _currentGame = null;
                Become(InLobby);
            });
        }

        private bool IsForMe(Message.GameMessage message)
        {
            return message.Token == _token && message.GameToken == _currentGameToken;
        }

        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(ex => Directive.Resume);
        }
    }
}
