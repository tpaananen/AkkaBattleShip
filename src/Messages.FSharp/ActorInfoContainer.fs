namespace Messages.FSharp
    open System
    open Akka.Actor
    type ActorInfoContainer(name:string, actorRef:IActorRef, token:Guid) =
        member this.Name = name
        member this.Actor = actorRef
        member this.Token = token
