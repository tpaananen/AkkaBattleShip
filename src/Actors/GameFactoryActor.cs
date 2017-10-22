using System;
using System.Collections.Generic;
using Akka.Actor;
using Messages.FSharp;

namespace Actors.CSharp
{
    public class GameFactoryActor : BattleShipActor
    {
        private readonly IDictionary<Guid, IActorRef> _activeGames = new Dictionary<Guid, IActorRef>();
        private readonly IDictionary<Guid, Guid> _activePlayers = new Dictionary<Guid, Guid>();

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

            Receive<PlayerArrived>(message =>
            {
                _currentGameToken = Guid.NewGuid();
                Log.Info("The first player arrived, forwarding to game with token " + _currentGameToken);

                _gameUnderConstruction = Context.ActorOf(Props.Create(() => new GameActor(_currentGameToken)), _currentGameToken.ToString());
                _activeGames[_currentGameToken] = _gameUnderConstruction;
                _activePlayers[message.Player.Token] = _currentGameToken;
                _gameUnderConstruction.Tell(new PlayerJoining(message.Player.Token, _currentGameToken, message.Player), Self);
                Become(WaitingForSecondPlayer);
            });

            Receive<StopGame>(message => StopGame(message));
            Receive<PlayersFree>(message => FreePlayers(message));
            Receive<PlayerTerminated>(message => FindGameAndStop(message));
        }

        private void WaitingForSecondPlayer()
        {
            Receive<PlayerArrived>(message =>
            {
                Log.Info("The second player arrived, forwarding to game with token " + _currentGameToken);
                _activePlayers[message.Player.Token] = _currentGameToken;
                _gameUnderConstruction.Tell(new PlayerJoining(message.Player.Token, _currentGameToken, message.Player), Self);
                Become(WaitingForFirstPlayer);
            });

            Receive<StopGame>(message => StopGame(message));
            Receive<PlayersFree>(message => FreePlayers(message));
            Receive<PlayerTerminated>(message => FindGameAndStop(message));
        }

        protected override void PreRestart(Exception reason, object message)
        {
            foreach (var game in _activeGames)
            {
                StopGame(new StopGame(Guid.Empty, game.Key));
            }
            base.PreRestart(reason, message);
        }

        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(x => Directive.Resume);
        }

        private void StopGame(StopGame message)
        {
            if (_activeGames.TryGetValue(message.GameToken, out var game))
            {
                game.Tell(message, Self);
                RemoveGame(message.GameToken);
            }
        }

        private void FindGameAndStop(PlayerTerminated message)
        {
            if (_activePlayers.TryGetValue(message.Token, out var gameToken))
            {
                StopGame(new StopGame(message.Token, gameToken));
            }
        }

        private void FreePlayers(PlayersFree message)
        {
            Context.Parent.Forward(message);
            RemoveGame(message.GameToken);
            RemovePlayers(message.Tokens);
        }

        private void RemovePlayers(IEnumerable<Guid> tokens)
        {
            foreach (var token in tokens)
            {
                _activePlayers.Remove(token);
            }
        }

        private void RemoveGame(Guid gameToken)
        {
            _activeGames.Remove(gameToken);
        }
    }
}
