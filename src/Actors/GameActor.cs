using System;
using Akka.Actor;
using Messages.CSharp;
using Messages.CSharp.Containers;
using Messages.CSharp.Pieces;

// ReSharper disable PossibleUnintendedReferenceComparison

namespace Actors.CSharp
{
    public class GameActor : BattleShipActor
    {
        private ActorInfoContainer _player1;
        private ActorInfoContainer _player2;

        private IActorRef _player1Table;
        private IActorRef _player2Table;

        private bool _player1Initialized;
        private bool _player2Initialized;

        private readonly Guid _gameToken;

        public GameActor(Guid gameToken)
        {
            _gameToken = gameToken;

            Receive<MessagePlayerJoining>(message => message.GameToken == _gameToken, message =>
            {
                Log.Debug("Player 1 arrived");
                _player1 = message.Player;
                _player1.Actor.Tell(new MessageGameStatusUpdate(_player1.Token, _gameToken, GameStatus.Created, Self));

                Become(WaitingForSecondPlayer);
            });

            ReceiveAny(message =>
            {
                Log.Debug("Unhandled message of type " + message.GetType() + " received in intial state...");
            });
        }

        private void WaitingForSecondPlayer()
        {
            Receive<MessagePlayerJoining>(message => message.GameToken == _gameToken && message.Player != _player1, message =>
            {
                Log.Debug("Player 2 arrived");
                _player2 = message.Player;

                _player2.Actor.Tell(new MessageGameStatusUpdate(_player2.Token, _gameToken, GameStatus.PlayerJoined, Self));

                _player1.Actor.Tell(new MessageGiveMeYourPositions(_player1.Token, _gameToken), Self);
                _player2.Actor.Tell(new MessageGiveMeYourPositions(_player2.Token, _gameToken), Self);

                Become(WaitingForPositions);
            });

            ReceiveAny(message =>
            {
                Log.Debug("Unhandled message of type " + message.GetType() + " received in WaitingForSecondPlayer state...");
            });
        }

        private void WaitingForPositions()
        {
            Receive<MessagePlayerPositions>(message => message.GameToken == _gameToken && message.Token == _player1.Token && !_player1Initialized, message =>
            {
                Log.Debug("Player 1 ships have arrived");
                _player1Initialized = true;
                _player1Table = Context.ActorOf(Props.Create(() => new GameTableActor(message.Ships)), "P1:" + _player1.Name);

                if (_player2Initialized)
                {
                    _player1.Actor.Tell(new MessageGameStatusUpdate(_player1.Token, _gameToken, GameStatus.GameStartedOpponentStarts, Self), Self);
                    _player2.Actor.Tell(new MessageGameStatusUpdate(_player2.Token, _gameToken, GameStatus.GameStartedYouStart, Self), Self);
                    Become(PlayerTwo);
                }
            });

            Receive<MessagePlayerPositions>(message => message.GameToken == _gameToken && message.Token == _player2.Token && !_player2Initialized, message =>
            {
                Log.Debug("Player 2 ships have arrived");
                _player2Initialized = true;
                _player2Table = Context.ActorOf(Props.Create(() => new GameTableActor(message.Ships)), "P2:" + _player2.Name);

                if (_player1Initialized)
                {
                    _player1.Actor.Tell(new MessageGameStatusUpdate(_player1.Token, _gameToken, GameStatus.GameStartedYouStart, Self), Self);
                    _player2.Actor.Tell(new MessageGameStatusUpdate(_player2.Token, _gameToken, GameStatus.GameStartedOpponentStarts, Self), Self);
                    Become(PlayerOne);
                }
            });

            ReceiveAny(message =>
            {
                Log.Debug("Unhandled message of type " + message.GetType() + " received in WaitingForPositions state...");
            });
        }

        private void PlayerOne()
        {
            #region Player message

            Receive<MessageMissile>(message => message.Token == _player1.Token && message.GameToken == _gameToken, message =>
            {
                _player2Table.Tell(message, Self);
            });

            #endregion

            #region Table responses

            Receive<Point[]>(message =>
            {
                _player2.Actor.Tell(new MessageTable(_player2.Token, _gameToken, message), Self);
                Become(PlayerTwo);
            });

            Receive<MessageGameOver>(message =>
            {
                _player2.Actor.Tell(new MessageGameStatusUpdate(_player1.Token, _gameToken, GameStatus.YouLost, Self), Self);
                _player1.Actor.Tell(new MessageGameStatusUpdate(_player1.Token, _gameToken, GameStatus.YouWon, Self), Self);
                Become(GameOver);
            });

            Receive<MessageAlreadyHit>(message =>
            {
                _player1.Actor.Tell(new MessageMissileAlreadyHit(_player1.Token, _gameToken, message.Point), Self);
            });

            #endregion

            ReceiveAny(message =>
            {
                Log.Debug("Unhandled message of type " + message.GetType() + " received in PlayerOne state...");
            });
        }

        private void PlayerTwo()
        {
            #region Player message

            Receive<MessageMissile>(message => message.Token == _player2.Token && message.GameToken == _gameToken, message =>
            {
                _player1Table.Tell(message, Self);
            });

            #endregion

            #region Table responses

            Receive<Point[]>(message =>
            {
                _player1.Actor.Tell(new MessageTable(_player1.Token, _gameToken, message), Self);
                Become(PlayerOne);
            });

            Receive<MessageGameOver>(message =>
            {
                _player2.Actor.Tell(new MessageGameStatusUpdate(_player1.Token, _gameToken, GameStatus.YouWon, Self), Self);
                _player1.Actor.Tell(new MessageGameStatusUpdate(_player1.Token, _gameToken, GameStatus.YouLost, Self), Self);
                Become(GameOver);
            });

            Receive<MessageAlreadyHit>(message =>
            {
                _player2.Actor.Tell(new MessageMissileAlreadyHit(_player2.Token, _gameToken, message.Point), Self);
            });

            #endregion

            ReceiveAny(message =>
            {
                Log.Debug("Unhandled message of type " + message.GetType() + " received in PlayerTwo state...");
            });
        }

        private void GameOver()
        {
            ActorSystemContext.VirtualManager().Tell(new MessagePlayersFree(_player1.Token, _player2.Token), Self);
            Context.Parent.Tell(new MessageGameStatusUpdate(Guid.Empty, _gameToken, GameStatus.GameOver, Self), Self);
            
            ReceiveAny(message =>
            {
                Log.Debug("Unhandled message of type " + message.GetType() + " received in GameOver state...");
            });
        }

    }
}
