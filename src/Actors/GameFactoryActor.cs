﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Actor;
using Messages.CSharp;

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

            Receive<Message.PlayerArrived>(message =>
            {
                _currentGameToken = Guid.NewGuid();
                Log.Info("The first player arrived, forwarding to game with token " + _currentGameToken);

                _gameUnderConstruction = Context.ActorOf(Props.Create(() => new GameActor(_currentGameToken)), _currentGameToken.ToString());
                _activeGames[_currentGameToken] = _gameUnderConstruction;
                _activePlayers[message.Player.Token] = _currentGameToken;
                _gameUnderConstruction.Tell(new Message.PlayerJoining(_currentGameToken, message.Player), Self);
                Become(WaitingForSecondPlayer);
            });

            Receive<Message.StopGame>(message => StopGame(message));
            Receive<Message.PlayersFree>(message => FreePlayers(message));
            Receive<Message.PlayerTerminated>(message => FindGameAndStop(message));
        }

        private void WaitingForSecondPlayer()
        {
            Receive<Message.PlayerArrived>(message =>
            {
                Log.Info("The second player arrived, forwarding to game with token " + _currentGameToken);
                _activePlayers[message.Player.Token] = _currentGameToken;
                _gameUnderConstruction.Tell(new Message.PlayerJoining(_currentGameToken, message.Player), Self);
                Become(WaitingForFirstPlayer);
            });

            Receive<Message.StopGame>(message => StopGame(message));
            Receive<Message.PlayersFree>(message => FreePlayers(message));
            Receive<Message.PlayerTerminated>(message => FindGameAndStop(message));
        }

        protected override void PreRestart(Exception reason, object message)
        {
            foreach (var game in _activeGames)
            {
                StopGame(new Message.StopGame(Guid.Empty, game.Key));
            }
            base.PreRestart(reason, message);
        }

        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(x => Directive.Resume);
        }

        private void StopGame(Message.StopGame message)
        {
            IActorRef game;
            if (_activeGames.TryGetValue(message.GameToken, out game))
            {
                game.Tell(message, Self);
                RemoveGame(message.GameToken);
            }
        }

        private void FindGameAndStop(Message.PlayerTerminated message)
        {
            Guid gameToken;
            if (_activePlayers.TryGetValue(message.Token, out gameToken))
            {
                StopGame(new Message.StopGame(message.Token, gameToken));
            }
        }

        private void FreePlayers(Message.PlayersFree message)
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
