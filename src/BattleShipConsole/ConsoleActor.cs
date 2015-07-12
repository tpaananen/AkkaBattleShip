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
            Receive<Message.GiveMeYourPositions>(message =>
            {
                Tell("Coordinates format: A1:A5 -> a length of 4 ship vertically positioned");
                _stack = new Stack<Tuple<string, int>>(message.Ships);
                Become(GettingPoints);
            });

            Receive<string>(message => message == "coord", message =>
            {
                Tell("Your turn, give the next position to hit (format: A10) : ");
                var position = Read();
                Point point;
                if (!_pointReader.ParsePoint(position, out point))
                {
                    Self.Tell("coord");
                    return;
                }
                Context.Parent.Tell(point, Self);
            });

            Receive<string>(message => message == "join", message =>
            {
                Tell("Press [J] to join to a game, [E] to exit: ");
                var response = Read();
                if (string.Compare(response, "J", StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    Context.Parent.Tell("join", Self);
                }
                else if (string.Compare(response, "E", StringComparison.InvariantCultureIgnoreCase) == 0)
                {
                    Context.Parent.Tell("unregister", Self);
                }
                else
                {
                    Tell("Invalid option '" + response + "'");
                    Self.Tell("join");
                }
            });

            Receive<Message.GameTable>(message =>
            {
                _tableWriter.ShowTable(message.Points);
            });

            Receive<string>(message =>
            {
                Tell(message);
            });
        }

        private void GettingPoints()
        {
            Receive<string>(message => message == "get" && _stack.Count != 0, message =>
            {
                var item = _stack.Pop();
                Tell("Give coordinates for " + item.Item1 + " (len: " + item.Item2 + ": ");
                var coords = Read();
                if (!_pointReader.CreateShip(coords, item.Item2, _selectedShips))
                {
                    _stack.Push(item);
                }
                Self.Tell("get");
            });

            Receive<string>(message => message == "get" && _stack.Count == 0, message =>
            {
                Context.Parent.Tell(new Message.ShipPositions(Guid.Empty, Guid.Empty, _selectedShips), Self);
                Become(Idle);
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

        private static string Read()
        {
            return Console.ReadLine();
        }
    }
}
