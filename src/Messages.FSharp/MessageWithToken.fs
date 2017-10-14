namespace Messages.FSharp
    [<AbstractClass>]
    type WithToken(token:System.Guid) =
        member this.Token = token
