module Messages.FSharp.TablesAndShips

    type Piece = {
        Name:string
        Length:int}

    let private pieces = [
        { Name = "Carrier"; Length = 5 }
        { Name = "BattleShip"; Length = 4 }
        { Name = "Cruiser"; Length = 3 }
        { Name = "Cruiser"; Length = 3 }
        { Name = "Destroyer"; Length = 2 }
        { Name = "Submarine"; Length = 1 }
    ]
    
    let GetTablesAndShips() = pieces
