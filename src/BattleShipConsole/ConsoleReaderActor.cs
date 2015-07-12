using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Actors.CSharp;
using Akka.Actor;

namespace BattleShipConsole
{
    public class ConsoleReaderActor : BattleShipActor
    {
        private readonly HashSet<IActorRef> _subscribers = new HashSet<IActorRef>();
        private readonly object _read = new object();

        public ConsoleReaderActor()
        {
            Receive<IActorRef>(writer =>
            {
                if (!_subscribers.Add(writer))
                {
                    _subscribers.Remove(writer);
                }
            });

            Receive<string>(message =>
            {
                foreach (var actorRef in _subscribers)
                {
                    actorRef.Tell(message);
                }
                Self.Tell(_read);
            });

            ReceiveAny(message =>
            {
                var self = Self;
                Task.Run(() => Console.ReadLine())
                    .ContinueWith(task =>
                    {
                        if (task.IsFaulted || task.IsCanceled)
                        {
                            return string.Empty;
                        }
                        return task.Result;
                    }, TaskContinuationOptions.AttachedToParent | TaskContinuationOptions.ExecuteSynchronously)
                    .PipeTo(self, self);
            });
        }

        protected override void PreStart()
        {
            Self.Tell(_read);
            base.PreStart();
        }
    }
}
