using System.Collections.Generic;
using Messages.CSharp;
using Messages.CSharp.Pieces;

namespace Actors.CSharp
{
    public class ShipActor : BattleShipActor
    {
        private readonly HashSet<Point> _points; 

        public ShipActor(Ship ship)
        {
            _points = new HashSet<Point>(ship.Points);

            Receive<MessagePartOfTheShipDestroyed>(message => _points.Contains(message.Point), message =>
            {
                _points.Remove(message.Point);
                if (_points.Count == 0)
                {
                    Context.Parent.Tell(new MessageShipDestroyed(), Self);
                    Become(Destroyed);
                }
                else
                {
                    Context.Parent.Tell(message, Self);
                }
            });
        }

        private void Destroyed()
        {
            ReceiveAny(message =>
            {
                Log.Error("Ship is already destroyed, not expecting any messages, but got " + message.GetType());
            });
        }

    }
}
