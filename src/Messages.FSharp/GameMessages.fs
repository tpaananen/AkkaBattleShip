namespace Messages.FSharp
    open System
    open Akka.Actor
    open Messages.FSharp
    open Messages.FSharp.TablesAndShips
    open System.Collections.Generic

    [<AbstractClass>]
    type GameMessage(token, gameToken: Guid) =
        inherit WithToken(token)
        member this.GameToken = gameToken
    
    [<AbstractClass>]
    type WithPoint(token, gameToken, point:Point) = 
        inherit GameMessage(token, gameToken)
        member this.Point = point

    type CreateGame(token: System.Guid) =
        inherit WithToken(token)

    type UnableToCreateGame(token, error:string) =
        inherit WithToken(token)
        member this.Error = error
    
    type PlayerArrived(player:ActorInfoContainer) =
        member this.Player = player

    type GameStatusUpdate(token, gameToken, status:GameStatus, game:IActorRef, message:string) =
        inherit GameMessage(token, gameToken)
        member this.Game = game
        member this.Status = status
        member this.Message = message

    type PlayerJoining(token, gameToken, player:ActorInfoContainer) =
        inherit GameMessage(token, gameToken)
        member this.Player = player

    type ShipPosition(token, gameToken, ship:Ship) =
        inherit GameMessage(token, gameToken)
        member this.Ship = ship

    type GiveMeNextPosition(token, gameToken, config:Piece, error:string) =
        inherit GameMessage(token , gameToken)
        member this.Config = config
        member this.ErrorInPreviousConfig = error

    type Missile(token, gameToken, point) =
        inherit WithPoint(token, gameToken, point)

    type GameTable(token, gameToken, points:IReadOnlyList<Point>) = 
        inherit GameMessage(token, gameToken)
        member this.Points = points

    type MissileAlreadyHit(token, gameToken, point) = 
        inherit WithPoint(token, gameToken, point)

    type MissileWasAHit(token, gameToken, point, shipDestroyed:bool) = 
        inherit WithPoint(token, gameToken, point)
        member this.ShipDestroyed = shipDestroyed

    type StopGame(userToken, gameToken) = 
        inherit GameMessage(userToken, gameToken)
