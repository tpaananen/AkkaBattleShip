using System;
using System.Collections.Generic;
using Akka.Actor;
using Messages.CSharp;
using Messages.CSharp.Containers;

namespace Actors.CSharp
{
    public class GameManagerActor : BattleShipActor
    {
        private readonly Dictionary<IActorRef, ActorInfoContainer> _players = new Dictionary<IActorRef, ActorInfoContainer>(128);
        private readonly HashSet<Guid> _playerTokens = new HashSet<Guid>(); 
        private readonly IActorRef _gameFactory;

        public GameManagerActor()
        {
            _gameFactory = Context.ActorOf(Props.Create<GameFactoryActor>(), Guid.NewGuid().ToString());

            Receive<MessageRegisterPlayer>(HasSender, message =>
            {
                Log.Debug("Register message from " + message.Name);
                if (_players.ContainsKey(Sender))
                {
                    string error = "Received register player message ('" + message.Name + "'), but the sender is already registered.";
                    Log.Error(error);
                    Sender.Tell(new MessageRegisterPlayerResponse(Guid.Empty, false, error), Self);
                    return;
                }

                var container = CreateActorInfoContainer(message.Name, Sender);
                _players.Add(Sender, container);
                Sender.Tell(new MessageRegisterPlayerResponse(container.Token, true), Self);
            });

            Receive<MessageUnregisterPlayer>(HasSender, message =>
            {
                Log.Debug("Unregister message from " + message.Token);
                if (!_players.ContainsKey(Sender))
                {
                    Log.Error("Received message to unregister a player, but the player does not exist with the token");
                    return;
                }
                _players.Remove(Sender);
            });

            Receive<MessageCreateGame>(HasSender, message =>
            {
                Log.Debug("Create game message from " + message.Token);

                string error;
                if (!_playerTokens.Add(message.Token))
                {
                    error = "The player with the same token is already in a game.";
                    Log.Error(error);
                    Sender.Tell(new MessageUnableToCreateGame(message.Token, error), Self);
                    return;
                }

                var player = GetPlayer(message, out error);
                if (error != null)
                {
                    Log.Error(error);
                    Sender.Tell(new MessageUnableToCreateGame(message.Token, error), Self);
                    return;
                }

                _gameFactory.Tell(new MessagePlayerArrived(player), Self);
            });

            Receive<MessagePlayersFree>(HasSender, message =>
            {
                Log.Debug("Players free message from game " + message.GameToken);
                foreach (var token in message.Tokens)
                {
                    _playerTokens.Remove(token);
                }
            });
        }

        #region Helpers

        private ActorInfoContainer GetPlayer(MessageWithToken message, out string error)
        {
            error = null;
            ActorInfoContainer player;
            if (!_players.TryGetValue(Sender, out player) || player.Token != message.Token)
            {
                error = "Received message create game from unregistered player.";
            }
            return player;
        }

        private static ActorInfoContainer CreateActorInfoContainer(string name, IActorRef sender)
        {
            return new ActorInfoContainer(name, sender, Guid.NewGuid());
        }

        #endregion
    }
}
