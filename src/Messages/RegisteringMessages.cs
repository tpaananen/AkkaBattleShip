using System;
using Akka.Actor;

namespace Messages.CSharp
{
    public abstract partial class Message
    {

        public class RegisterPlayer
        {
            public string Name { get; private set; }

            public RegisterPlayer(string name)
            {
                Name = name;
            }
        }

        public class RegisterPlayerResponse
        {
            public Guid Token { get; private set; }

            public bool IsValid { get; private set; }

            public string Errors { get; private set; }

            public RegisterPlayerResponse(Guid token, bool isValid, string errors = null)
            {
                Token = token;
                IsValid = isValid;
                Errors = errors;
            }
        }

        public class UnregisterPlayer : GameMessage
        {
            public UnregisterPlayer(Guid token, Guid gameToken) 
                : base(token, gameToken)
            {
            }
        }

        public class CreatePlayer
        {
            public string Name { get; private set; }

            public CreatePlayer(string name)
            {
                Name = name;
            }
        }
    }
}
