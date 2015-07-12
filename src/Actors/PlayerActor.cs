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
            _playerUserInterface.Tell(PoisonPill.Instance, Self);
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

            Receive<Message.GameStatusUpdate>(message => message.Status == GameStatus.Created, message =>
            {
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
            });

            Receive<Message.GameStatusUpdate>(message => message.Status == GameStatus.PlayerJoined, message =>
            {
                _playerUserInterface.Tell(message.Message, Self);
            });

            Receive<Message.GameStatusUpdate>(message => message.Status == GameStatus.YouLost, message =>
            {
                _playerUserInterface.Tell(message.Message, Self);
                Become(InLobby);
            });

            Receive<Message.GiveMeYourPositions>(message =>
            {
                if (message.GameToken == _currentGameToken)
                {
                    _playerUserInterface.Tell(message, Self);
                }
            });

            Receive<Message.ShipPositions>(message =>
            {
                _currentGame.Tell(new Message.ShipPositions(_token, _currentGameToken, message.Ships), Self);
                Become(WaitingForGameStart);
            });

            Receive<string>(message => message == "unregister", message =>
            {
                _gameManager.Tell(new Message.UnregisterPlayer(_token, _currentGameToken), Self);
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
            Receive<Message.GameStatusUpdate>(message =>
            {
                if (message.GameToken != _currentGameToken)
                {
                    Log.Debug("Invalid game token in GameStatusUpdate");
                    return;
                }

                if (message.Status == GameStatus.GameStartedOpponentStarts)
                {
                    _playerUserInterface.Tell("Opponent starts...");
                }
                else if (message.Status == GameStatus.GameStartedYouStart)
                {
                    _playerUserInterface.Tell("coord", Self);                    
                }
                else
                {
                    _currentGame = null;
                    _currentGameToken = Guid.Empty;
                    if (!string.IsNullOrEmpty(message.Message))
                    {
                        _playerUserInterface.Tell(message.Message, Self);
                    }
                    Become(InLobby);
                    return;
                }
                Become(GameStarted);
            });

            ReceiveAny(message =>
            {
                Log.Debug("Unhandled message of type " + message.GetType() + " received in WaitingForGameStart state...");
            });
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
                if (message.Status != GameStatus.GameOver)
                {
                    _playerUserInterface.Tell(message.Status == GameStatus.YouWon ? "You won!" : "You lost!", Self);
                }
                else
                {
                    _playerUserInterface.Tell(message.Message, Self);
                }
                _currentGameToken = Guid.Empty;
                _currentGame = null;
                Become(InLobby);
            });

            ReceiveAny(message =>
            {
                Log.Debug("Unhandled message of type " + message.GetType() + " received in GameStarted state...");
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
