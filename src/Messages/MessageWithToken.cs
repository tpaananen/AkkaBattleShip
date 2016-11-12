using System;

namespace Messages.CSharp
{
    public abstract partial class Message
    {
        public abstract class WithToken
        {

            public Guid Token { get; private set; }

            protected WithToken(Guid token)
            {
                Token = token;
            }
        }
    }
}
