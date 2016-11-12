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
            _gameFactory = Context.ActorOf(Props.Create<GameFactoryActor>(), "gameFactory");

            Receive<Message.RegisterPlayer>(HasSender, message =>
            {
                Log.Info("Register message from " + message.Name);
                if (_players.ContainsKey(Sender))
                {
                    string error = "Received register player message ('" + message.Name + "'), but the sender is already registered.";
                    Log.Error(error);
                    Sender.Tell(new Message.RegisterPlayerResponse(Guid.Empty, false, error), Self);
                    return;
                }

                var container = CreateActorInfoContainer(message.Name, Sender);
                _players.Add(Sender, container);
                Sender.Tell(new Message.RegisterPlayerResponse(container.Token, true), Self);
                Context.Watch(Sender);
            });

            Receive<Message.UnregisterPlayer>(HasSender, message =>
            {
                Log.Info("Unregister message from " + message.Token);
                if (!_players.Remove(Sender))
                {
                    Log.Error("Received message to unregister a player, but the player does not exist with the token " + message.Token);
                    return;
                }

                if (message.GameToken != Guid.Empty)
                {
                    _gameFactory.Tell(new Message.StopGame(message.Token, message.GameToken));
                }
                else
                {
                    _gameFactory.Tell(new Message.PlayerTerminated(message.Token), Self);
                }

                Context.Unwatch(Sender);
            });

            Receive<Message.CreateGame>(HasSender, message =>
            {
                Log.Info("Create game message from " + message.Token);

                string error;
                if (!_playerTokens.Add(message.Token))
                {
                    error = "The player with the same token is already in a game.";
                    Log.Error(error);
                    Sender.Tell(new Message.UnableToCreateGame(message.Token, error), Self);
                    return;
                }

                var player = GetPlayer(message, out error);
                if (error != null)
                {
                    Log.Error(error);
                    Sender.Tell(new Message.UnableToCreateGame(message.Token, error), Self);
                    return;
                }

                _gameFactory.Tell(new Message.PlayerArrived(player), Self);
            });

            Receive<Message.PlayersFree>(HasSender, message =>
            {
                Log.Info("Players free message from game " + message.GameToken);
                foreach (var token in message.Tokens)
                {
                    _playerTokens.Remove(token);
                }
            });

            Receive<Terminated>(message =>
            {
                ActorInfoContainer container;
                if (_players.TryGetValue(message.ActorRef, out container))
                {
                    Log.Info($"Remote player {container.Name} terminated, removing...");
                    _players.Remove(message.ActorRef);
                    _gameFactory.Tell(new Message.PlayerTerminated(container.Token), Self);
                }
                Context.Unwatch(message.ActorRef);
            });
        }

        #region Helpers

        private ActorInfoContainer GetPlayer(Message.WithToken message, out string error)
        {
            error = null;
            ActorInfoContainer player;
            if (!_players.TryGetValue(Sender, out player) || player.Token != message.Token)
            {
                error = "Received a message to create a game from unregistered player.";
            }
            return player;
        }

        private static ActorInfoContainer CreateActorInfoContainer(string name, IActorRef sender)
        {
            return new ActorInfoContainer(name, sender, Guid.NewGuid());
        }

        #endregion

        protected override SupervisorStrategy SupervisorStrategy()
        {
            return new OneForOneStrategy(x =>
            {
                Log.Error(x.Message);
                return Directive.Restart;
            });
        }
    }
}
