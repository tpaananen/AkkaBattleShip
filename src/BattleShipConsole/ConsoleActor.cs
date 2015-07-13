using System;
using System.Text.RegularExpressions;
using Actors.CSharp;
using Akka.Actor;
using Messages.CSharp;
using Messages.CSharp.Pieces;

namespace BattleShipConsole
{
    public class ConsoleActor : BattleShipActor
    {
        private readonly TableWriter _tableWriter = new TableWriter();
        private readonly PointReader _pointReader = new PointReader();

        private static readonly Regex ShipMatcher = new Regex("^[a-jA-J]{1}([1-9]|10){1}[:]{1}[a-jA-J]{1}([1-9]|10){1}$", RegexOptions.Compiled);
        private static readonly Regex PointMatcher = new Regex("^[a-jA-J]{1}([1-9]|10){1}$", RegexOptions.Compiled);

        public ConsoleActor()
        {
            Become(Idle);
        }

        private void Idle()
        {
            Receive<string>(message => message == "join", message =>
            {
                Become(WaitingOption);
                Tell("Press [J] to join to a game, [E] to exit: ");
            });

            Receive<string>(message =>
            {
                Tell(message);
            });

            Receive<Message.GameStatusUpdate>(message =>
            {
                HandleStatusMessage(message);
            });
        }

        private void WaitingOption()
        {
            Receive<string>(message =>
            {
                if (string.Compare(message, "J", StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    Become(Waiting);
                    Context.Parent.Tell("join", Self);
                }
                else if (string.Compare(message, "E", StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    Become(Idle);
                    Context.Parent.Tell("unregister", Self);
                }
                else
                {
                    Tell("Invalid option '" + message + "'");
                    Become(Idle);
                    Self.Tell("join");
                }
            });

            Receive<Message.GameStatusUpdate>(message =>
            {
                HandleStatusMessage(message);
            });
        }

        private void Waiting()
        {
            Receive<Message.GiveMeNextPosition>(message =>
            {
                if (!string.IsNullOrEmpty(message.ErrorInPreviousConfig))
                {
                    Tell(message.ErrorInPreviousConfig);
                }
                Tell("Coordinates format: A1:A5 -> a length of 5 ship vertically positioned");
                Tell("Give coordinates for " + message.Config.Item1 + " (len: " + message.Config.Item2 + "): ");
            });

            Receive<string>(message => message == "coord" && Sender == Context.Parent, message =>
            {
                Tell("Your turn, give the next position to hit (format: A10) : ");
            });

            Receive<Message.GameTable>(message =>
            {
                _tableWriter.ShowTable(message.Points);
            });

            Receive<Message.GameStatusUpdate>(message =>
            {
                HandleStatusMessage(message);
            });

            Receive<string>(message => ShipMatcher.IsMatch(message), message =>
            {
                var ship = _pointReader.CreateShip(message);
                if (ship != null)
                {
                    Context.Parent.Tell(new Message.ShipPosition(Guid.Empty, Guid.Empty, ship), Self);
                    return;
                }
                Tell("Invalid ship config, try again");
            });

            Receive<string>(message => PointMatcher.IsMatch(message), message =>
            {
                Point point;
                if (!_pointReader.ParsePoint(message, out point))
                {
                    Become(Idle);
                    Self.Tell("coord");
                    return;
                }
                Become(Waiting);
                Context.Parent.Tell(point, Self);
            });
        }

        private void HandleStatusMessage(Message.GameStatusUpdate message)
        {
            if (!string.IsNullOrEmpty(message.Message))
            {
                Tell(message.Message);
            }
            if (message.Status == GameStatus.GameOver || message.Status == GameStatus.YouLost ||
                message.Status == GameStatus.YouWon)
            {
                if (message.Status != GameStatus.GameOver)
                {
                    Tell(message.Status == GameStatus.YouWon ? "You won!" : "You lost!");
                }
                Become(Idle);
            }
        }

        protected override void PreRestart(Exception reason, object message)
        {
            Self.Tell(message);
            base.PreRestart(reason, message);
        }

        private static void Tell(string line)
        {
            Console.WriteLine(line);
        }
    }
}
