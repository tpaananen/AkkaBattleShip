﻿using System;
using Akka.Actor;
using Messages.CSharp;
using Messages.CSharp.Pieces;

namespace Actors.CSharp
{
    public class PointActor : BattleShipActor
    {
        private Point _point;

        private IActorRef _ship;

        private readonly Guid _gameToken;

        public PointActor(Point point, Guid gameToken)
        {
            _point = point;
            _gameToken = gameToken;

            Receive<MessageYouArePartOfShip>(message =>
            {
                _ship = message.ShipActorRef;
            });

            Receive<MessageMissile>(message => message.Point == _point, message =>
            {
                PointHasHit();
                if (_ship != null)
                {
                    _ship.Tell(new MessagePartOfTheShipDestroyed(Guid.Empty, _gameToken, _point), Self);
                }
                else
                {
                    Context.Parent.Tell(new MessageMissileDidNotHitShip(Guid.Empty, _gameToken, message.Point), Self);
                }
                Become(Destroyed);
            });
        }

        private void Destroyed()
        {
            Receive<MessageMissile>(message =>
            {
                Context.Parent.Tell(new MessageAlreadyHit(Guid.Empty, _gameToken, _point), Self);
            });
        }

        private void PointHasHit()
        {
            _point = new Point(_point.X, _point.Y, _point.HasShip, true);
        }
    }
}
