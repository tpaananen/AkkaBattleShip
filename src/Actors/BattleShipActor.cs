using System;
using Akka.Actor;
using Akka.Event;

namespace Actors.CSharp
{
    public abstract class BattleShipActor : ReceiveActor
    {
        protected readonly ILoggingAdapter Log = Context.GetLogger();

        protected Predicate<object> HasSender
        {
            get { return message => Sender != null; }
        }

    }
}
