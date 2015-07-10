using System;
using System.Collections.Generic;
using System.Linq;
using Actors.CSharp;
using Akka.Actor;
using Messages.CSharp;
using Messages.CSharp.Pieces;

namespace BattleShipConsole
{
    public class ConsoleActor : BattleShipActor
    {
        private Stack<Tuple<string, int>> _stack = new Stack<Tuple<string, int>>();
        private List<Ship> _selectedShips;

        public ConsoleActor()
        {
            Become(Idle);
        }

        private void Idle()
        {
            Receive<MessageGiveMeYourPositions>(message =>
            {
                Tell("Coordinates format: A1:A5 -> a length of 4 ship vertically positioned");
                Become(GettingPoints);
            });

            Receive<string>(message => message == "coord", message =>
            {
                Tell("Your turn, give the next position to hit (format: A10) : ");
                var position = Read();
                Point point;
                if (!ParsePoint(position, out point))
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

            Receive<MessageTable>(message =>
            {
                Tell("Battleships:");
                Console.Write("  ");
                for (int i = 65; i < 75; ++i)
                {
                    Console.Write((char)i);
                }
                Tell("");

                var points = message.Points;
                for (int i = 0; i < 10; ++i)
                {
                    Console.Write((i + 1).ToString().PadLeft(2, ' '));
                    for (int j = 0; j < 10; ++j)
                    {
                        var index = i * 10 + j;
                        var point = points[index];

                        if (point.HasShip && !point.HasHit)
                        {
                            Console.BackgroundColor = ConsoleColor.Gray;
                        }
                        else if (point.HasShip && point.HasHit)
                        {
                            Console.BackgroundColor = ConsoleColor.DarkMagenta;
                        }
                        else
                        {
                            Console.ResetColor();
                        }
                        Console.Write(point.HasHit ? "X" : " ");
                    }
                    Tell("");
                }
                Console.ResetColor();
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
                var item = _stack.Peek();
                Tell("Give coordinates for " + item.Item1 + " (len: " + item.Item2 + ": ");
                var coords = Read();
                if (PushCoords(coords, item.Item2))
                {
                    _stack.Pop();
                }
                Self.Tell("get");
            });

            Receive<string>(message => message == "get" && _stack.Count == 0, message =>
            {
                Context.Parent.Tell(new MessagePlayerPositions(Guid.Empty, Guid.Empty, _selectedShips), Self);
                Become(Idle);
            });

            _selectedShips = new List<Ship>();
            _stack = new Stack<Tuple<string, int>>(TablesAndShips.Ships);
            Self.Tell("get");
        }

        private bool PushCoords(string coords, int len)
        {
            var split = coords.Split(new [] { ":" }, StringSplitOptions.RemoveEmptyEntries);
            if (split.Length != 2)
            {
                return false;
            }

            Point startPoint;
            if (!ParsePoint(split[0], out startPoint))
            {
                return false;
            }

            Point endPoint;
            if (!ParsePoint(split[1], out endPoint))
            {
                return false;
            }

            if (startPoint - endPoint != len)
            {
                Tell("Length of the ship is not " + len + ", but " + (startPoint - endPoint));
                return false;
            }

            return FillAndPushPoints(startPoint, endPoint);
        }

        private bool FillAndPushPoints(Point startPoint, Point endPoint)
        {
            var list = new List<Point> {startPoint};
            if (startPoint != endPoint)
            {
                list.Add(endPoint);
            }
            list.Sort();

            if (list.Count > 1)
            {
                if (list[0].X == list[1].X)
                {
                    // vertical
                    for (var y = (byte) (list[0].Y + 1); y < list[1].Y; ++y)
                    {
                        list.Add(new Point(list[0].X, y, true));
                    }
                }
                else
                {
                    // Horiz
                    for (var x = (byte) (list[0].X + 1); x < list[1].X; ++x)
                    {
                        list.Add(new Point(x, list[0].Y, true));
                    }
                }

                var points = _selectedShips.SelectMany(x => x.Points);
                foreach (var point in points)
                {
                    if (list.Contains(point))
                    {
                        Tell("The point " + point + " already exists, overlapping ships are not allowed.");
                        return false;
                    }
                }
            }

            var ship = new Ship(list);
            _selectedShips.Add(ship);
            return true;
        }

        private static bool ParsePoint(string start, out Point point)
        {
            point = new Point(0, 0);
            if (start.Length < 2 || start.Length > 3)
            {
                Tell("Invalid number of characters in " + start);
                return false;
            }
            var c = (byte) ((byte)start.Substring(0, 1).ToUpper()[0] - (byte)'A' + 1);
            var n = byte.Parse(start.Substring(1));

            if (c < 1 || c > 10)
            {
                Tell("Invalid range in " + start);
                return false;
            }

            if (n < 1 || n > 10)
            {
                Tell("Invalid range in " + start);
                return false;
            }

            point = new Point(c, n);
            return true;
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
