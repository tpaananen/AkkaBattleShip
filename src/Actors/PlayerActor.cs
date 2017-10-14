using System;
using Akka.Actor;
using Messages.FSharp;
using Messages.FSharp.Message;
using Messages.FSharp.Pieces;

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

        private IActorRef _gameManager;
        private readonly IActorRef _reader;

        public PlayerActor(string name, Props propsForUserInteracting, IActorRef reader)
        {
            _reader = reader;
            _name = name;
            _playerUserInterface = Context.ActorOf(propsForUserInteracting, "ui");
            _reader.Tell(_playerUserInterface);
            Become(Unregistered);
        }

        protected override void PreRestart(Exception reason, object message)
        {
            _reader.Tell(_playerUserInterface);
            if (_token != Guid.Empty)
            {
                _gameManager?.Tell(new UnregisterPlayer(_token, _currentGameToken), Self);
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
                ActorSystemContext.VirtualManager().Tell(new RegisterPlayer(_name), Self);
            });

            Receive<RegisterPlayerResponse>(message =>
            {
                if (message.IsValid)
                {
                    _gameManager = Sender;
                    _token = message.Token;
                    _playerUserInterface.Tell("Registered to server with token " + message.Token, Self);
                    Become(InLobby);
                }
                else
                {
                    _playerUserInterface.Tell(message.Errors, Self);
                }
            });

            Receive<UnableToCreateGame>(message =>
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
                _gameManager.Tell(new CreateGame(_token), Self);
            });

            Receive<GameStatusUpdate>(message =>
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

            Receive<GiveMeNextPosition>(message =>
            {
                if (message.GameToken == _currentGameToken)
                {
                    _playerUserInterface.Tell(message, Self);
                }
            });

            Receive<ShipPosition>(message =>
            {
                if (_currentGame != null)
                {
                    _currentGame.Tell(new ShipPosition(_token, _currentGameToken, message.Ship), Self);
                }
            });

            Receive<GameTable>(IsForMe, message =>
            {
                _playerUserInterface.Tell(message, Self);
            });

            Receive<string>(message => message == "unregister", async message =>
            {
                _gameManager.Tell(new UnregisterPlayer(_token, _currentGameToken), Self);
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
            Receive<GameTable>(IsForMe, message =>
            {
                _playerUserInterface.Tell(message, Self);
            });

            Receive<GameStatusUpdate>(message => IsForMe(message) && message.Status == GameStatus.ItIsYourTurn, message =>
            {
                if (!string.IsNullOrEmpty(message.Message))
                {
                    _playerUserInterface.Tell(message.Message);
                }
                _playerUserInterface.Tell("coord", Self);
            });

            Receive<GameStatusUpdate>(message => IsForMe(message) && message.Status == GameStatus.None && !string.IsNullOrEmpty(message.Message), message =>
            {
                _playerUserInterface.Tell(message.Message, Self);
            });

            Receive<MissileAlreadyHit>(IsForMe, message =>
            {
                _playerUserInterface.Tell("The point was already used, sorry!", Self);
            });

            Receive<Point>(point =>
            {
                _currentGame.Tell(new Missile(_token, _currentGameToken, point), Self);
            });

            Receive<GameStatusUpdate>(message => IsForMe(message) && 
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

        private bool IsForMe(GameMessage message)
        {
            return message.Token == _token && message.GameToken == _currentGameToken;
        }

        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(ex => Directive.Resume);
        }
    }
}
