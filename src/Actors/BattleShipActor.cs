using Akka.Actor;
using Akka.Event;

namespace Actors.CSharp
{
    public abstract class BattleShipActor : ReceiveActor
    {
        private ILoggingAdapter _logger;

        protected ILoggingAdapter Log
        {
            get { return _logger ?? (_logger = Context.GetLogger()); }
        }

        protected bool HasSender(object message)
        {
            return Sender != null;
        }

    }
}
