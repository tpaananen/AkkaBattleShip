using System;
using System.Text.RegularExpressions;
using Actors.CSharp;
using Akka.Actor;
using Messages.FSharp;
using Messages.FSharp.Message;

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

            Receive<GameStatusUpdate>(message =>
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

            Receive<GameStatusUpdate>(message =>
            {
                HandleStatusMessage(message);
            });
        }

        private void Waiting()
        {
            Receive<GiveMeNextPosition>(message =>
            {
                if (!string.IsNullOrEmpty(message.ErrorInPreviousConfig))
                {
                    Tell(message.ErrorInPreviousConfig);
                }
                Tell("Coordinates format: A1:A5 -> a length of 5 ship vertically positioned");
                Tell("Give coordinates for " + message.Config.Name + " (len: " + message.Config.Length + "): ");
            });

            Receive<string>(message => message == "coord" && Sender == Context.Parent, message =>
            {
                Tell("Your turn, give the next position to hit (format: A10) : ");
            });

            Receive<GameTable>(message =>
            {
                _tableWriter.ShowTable(message.Points);
            });

            Receive<GameStatusUpdate>(message =>
            {
                HandleStatusMessage(message);
            });

            Receive<string>(message => ShipMatcher.IsMatch(message), message =>
            {
                try
                {
                    var ship = PointReader.CreateShip(message);
                    if (ship != null)
                    {
                        Context.Parent.Tell(new ShipPosition(Guid.Empty, Guid.Empty, ship), Self);
                        return;
                    }
                }
                catch (InvalidOperationException ex)
                {
                    Tell(ex.Message + " Invalid ship config, try again");
                    return;
                }
                Tell("Invalid ship config, try again");
            });

            Receive<string>(message => PointMatcher.IsMatch(message), message =>
            {
                if (!PointReader.ParsePoint(message, out var point))
                {
                    Become(Idle);
                    Self.Tell("coord");
                    return;
                }
                Become(Waiting);
                Context.Parent.Tell(point, Self);
            });

            Receive<string>(message =>
            {
                Tell(message);
            });
        }

        private void HandleStatusMessage(GameStatusUpdate message)
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
