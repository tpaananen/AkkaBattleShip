using System;
using System.Collections.Generic;
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
        private Stack<Tuple<string, int>> _stack;
        private List<Ship> _selectedShips;

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
        }

        private void Waiting()
        {
            Receive<Message.GiveMeYourPositions>(message =>
            {
                Tell("Coordinates format: A1:A5 -> a length of 4 ship vertically positioned");
                _stack = new Stack<Tuple<string, int>>(message.Ships);
                Become(GettingPoints);
            });

            Receive<Message.GameTable>(message =>
            {
                _tableWriter.ShowTable(message.Points);
            });

            Receive<string>(message => message == "coord", message =>
            {
                Tell("Your turn, give the next position to hit (format: A10) : ");
                Become(GettingSinglePoint);
            });
        }

        private void GettingSinglePoint()
        {
            Receive<string>(message =>
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

        private void GettingPoints()
        {
            Receive<string>(message => message == "get" && _stack.Count != 0, message =>
            {
                var item = _stack.Peek();
                Tell("Give coordinates for " + item.Item1 + " (len: " + item.Item2 + "): ");
            });

            Receive<string>(message => message == "get" && _stack.Count == 0, message =>
            {
                Become(Waiting);
                Context.Parent.Tell(new Message.ShipPositions(Guid.Empty, Guid.Empty, _selectedShips), Self);
                Tell("That's all, waiting for game to begin...");
            });

            Receive<string>(message =>
            {
                var item = _stack.Pop();
                if (!_pointReader.CreateShip(message, item.Item2, _selectedShips))
                {
                    _stack.Push(item);
                }
                Self.Tell("get");
            });

            _selectedShips = new List<Ship>();
            Self.Tell("get");
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
