namespace Messages.FSharp
    open System

    type RegisterPlayer(name:string) =
        member this.Name = name

    type RegisterPlayerResponse(token:Guid, isValid:bool, errors:string) =
        member this.Token = token
        member this.IsValid = isValid
        member this.Errors = errors

    type UnregisterPlayer(token, gameToken) = 
        inherit GameMessage(token, gameToken)

    type CreatePlayer(name:string) =
        member this.Name = name
