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

        public class UnregisterPlayer
        {
            public Guid Token { get; private set; }

            public UnregisterPlayer(Guid token)
            {
                Token = token;
            }
        }

        public class CreatePlayer
        {
            public Props Props { get; private set; }

            public CreatePlayer(Props props)
            {
                Props = props;
            }
        }
    }
}
