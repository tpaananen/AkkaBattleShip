namespace Messages.FSharp
    open System
    open System.Collections.Generic
    open System.Linq

    type Point(x:int, y:int, hasShip:bool, hasHit:bool) =
        member this.X = System.Char.ToUpper((char)x)
        member this.Y = y
        member this.HasShip = hasShip
        member this.HasHit = hasHit
        override this.ToString() = sprintf "[{%c}:{%d}], Ship: {%b}, Hit: {%b}" this.X this.Y this.HasShip this.HasHit;
        
        static member (-) (left:Point, right:Point) = 
            (((float)left.X - (float)'A') - ((float)right.X - (float)'A')) ** 2.0 + 
                ((float)left.Y - (float)right.Y) ** 2.0 
            |> sqrt

        member this.DistanceTo(other:Point):float =
            if this.Equals other then 1.0 else this - other + 1.0

        interface IComparable<Point> with
            member this.CompareTo(other:Point) =
                if this.Y = other.Y 
                then this.X.CompareTo(other.X) 
                else this.Y.CompareTo(other.Y);

        interface IComparable with
            member this.CompareTo obj =
                match obj with
                  | null                 -> 1
                  | :? Point as other -> (this :> IComparable<_>).CompareTo other
                  | _                    -> invalidArg "obj" "not a Point"
        
        interface IEquatable<Point> with
            member this.Equals(other:Point) =
                this.X = other.X && this.Y = other.Y;

        override this.Equals obj =
            match obj with
            | :? Point as other -> (this :> IEquatable<_>).Equals other
            | _                 -> false

        override this.GetHashCode() =
            hash (this.X, this.Y)

    type Ship(points:IReadOnlyList<Point>) =
        member this.Points = Ship.validate points
        member this.Length = Seq.distinct this.Points |> Seq.length

        static member private validate(points:IReadOnlyList<Point>):IReadOnlyList<Point> =
            let horizLen = points.Select(fun x -> x.X).Distinct().Count();
            let vertiLen = points.Select(fun y -> y.Y).Distinct().Count();

            if horizLen <> 1 && vertiLen <> 1 then
                raise (InvalidOperationException "Ship must be either vertical or horizontal.")
            
            let selector = 
                match horizLen with
                | 1 -> points.Select(fun point -> point.Y) 
                | _ -> points.Select(fun point -> (int)point.X)

            let pointsIndexes = selector.OrderBy(fun d -> d).ToArray();
            let previous = pointsIndexes.[0];
            
            let rec validate(previous, index) =
                if index < pointsIndexes.Length then
                    let current = pointsIndexes.[index]
                    if Math.Abs(current - previous) > 1
                    then raise (InvalidOperationException "Ship must not have holes.")
                    else validate(current, (index + 1))

            validate(previous, 1)

            points
