module Messages.FSharp.TablesAndShips
    open System
    open System.Collections.Generic

    type Piece(name:string, length:int) =
        member this.Name = name
        member this.Length = length

    let private pieces = [
        Piece("Carrier", 5)
        Piece("BattleShip", 4)
        Piece("Cruiser", 3)
        Piece("Cruiser", 3)
        Piece("Destroyer", 2)
        Piece("Submarine", 1)
    ]
    
    let GetTablesAndShips() = pieces
        
