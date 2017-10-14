namespace Messages.FSharp.Message
    type WithToken(token:System.Guid) =
        member this.Token = token
