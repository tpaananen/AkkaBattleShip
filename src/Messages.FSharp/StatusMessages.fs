namespace Messages.FSharp
    open System

    type ShipDestroyed(token, gameToken, point) =
        inherit WithPoint(token, gameToken, point)

    type MissileDidNotHitShip(token, gameToken, point) =
        inherit WithPoint(token, gameToken, point)

    type AlreadyHit(token, gameToken, point) =
        inherit WithPoint(token, gameToken, point)

    type PartOfTheShipDestroyed(token, gameToken, point) =
        inherit WithPoint(token, gameToken, point)

    type GameOver(token, gameToken) =
        inherit GameMessage(token, gameToken)

    type PlayersFree(gameToken:Guid, [<ParamArray>]tokens:Guid[]) =
        member this.GameToken = gameToken
        member this.Tokens = tokens

    type PlayerTerminated(token:Guid) =
        member this.Token = token
