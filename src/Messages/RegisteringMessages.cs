using System;
using Akka.Actor;

namespace Messages.CSharp
{
    public class MessageRegisterPlayer
    {
        public string Name { get; private set; }

        public MessageRegisterPlayer(string name)
        {
            Name = name;
        }
    }

    public class MessageRegisterPlayerResponse
    {
        public Guid Token { get; private set; }

        public bool IsValid { get; private set; }

        public string Errors { get; private set; }

        public MessageRegisterPlayerResponse(Guid token, bool isValid, string errors = null)
        {
            Token = token;
            IsValid = isValid;
            Errors = errors;
        }
    }

    public class MessageUnregisterPlayer
    {
        public Guid Token { get; private set; }

        public MessageUnregisterPlayer(Guid token)
        {
            Token = token;
        }
    }

    public class MessageCreatePlayer
    {
        public Props Props { get; private set; }

        public MessageCreatePlayer(Props props)
        {
            Props = props;
        }
    }

}
