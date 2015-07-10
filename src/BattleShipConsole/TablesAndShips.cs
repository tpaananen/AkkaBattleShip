using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BattleShipConsole
{
    internal static class TablesAndShips
    {
        
        public static readonly IReadOnlyCollection<Tuple<string, int>> Ships
            = new ReadOnlyCollection<Tuple<string, int>>(new List<Tuple<string, int>>
            {
                Tuple.Create("Carrier", 5),
                Tuple.Create("BattleShip", 4),
                Tuple.Create("Cruiser", 3),
                Tuple.Create("Cruiser", 3),
                Tuple.Create("Destroyer", 2),
                Tuple.Create("Submarine", 1)
            });


    }
}
