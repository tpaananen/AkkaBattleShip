using System;

namespace Messages.CSharp
{
    public abstract class MessageWithToken
    {

        public Guid Token { get; private set; }

        protected MessageWithToken(Guid token)
        {
            Token = token;
        }

    }
}
