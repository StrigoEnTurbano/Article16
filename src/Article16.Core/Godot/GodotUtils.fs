[<AutoOpen>]
module Godot.Utils

open Godot


open System.Runtime.CompilerServices

let inline asNode (node : Node) = node
let inline asControl (control : Control) = control

module Callable =
    let queue (action : _ -> unit) =
        Callable.From(fun () -> action ()).CallDeferred()

module GD = 
    let print what = GD.Print(what : string)
    let printf format =
        Printf.kprintf GD.Print format

    let load path : 'a = GD.Load<'a> path

    type Implements =
        static member _process action = {
            new Godot.Implements.Implement_Process() with
                override this._Process delta =
                    base._Process delta
                    action this delta
        }

        static member _physicsProcess action = {
            new Godot.Implements.Implement_PhysicsProcess() with
                override this._PhysicsProcess delta =
                    base._PhysicsProcess delta
                    action this delta
        }

        static member _notification (action) = {
            new Godot.Implements.Implement_Notification() with
                override this._Notification what = action this what
        }

        static member _input action = {
            new Godot.Implements.Implement_Input() with
                override this._Input event = action this event
        }
            
        static member _unhandledInput action = {
            new Godot.Implements.Implement_UnhandledInput() with
                override this._UnhandledInput event = action this event
        }

        static member _unhandledKeyInput action = {
            new Godot.Implements.Implement_UnhandledKeyInput() with
                override this._UnhandledKeyInput event = action this event
        }

        static member _ready action = {
            new Godot.Implements.Implement_Ready() with
                override this._Ready () = action this
        }

    let whenReady node action =
        if (node : Node).IsNodeReady() then action () else
        node.Ready'Handler <- IDisposable.handleSignalOneTime ^ fun _ _ ->
            action ()

type Color with
    member this.With () = this

module Corners =
    let inline clockwise looped start a b = [|
        yield start
        yield start + a
        yield start + a + b
        yield start + b
        if looped then
            yield start
    |]

type Vector2I with
    member this.With () = this
    member this.X_ = Vector2I(this.X, 0)
    member this._Y = Vector2I(0, this.Y)
    member this.YX = Vector2I(this.Y, this.X)
    member this.AsVector2 = Vector2(float32 this.X, float32 this.Y)

    member this.AsSeqIn = seq {
        for y = 0 to this.Y do
            for x = 0 to this.X do
                Vector2I(x, y)
    }
    member this.AsSeqEks = seq {
        for y = 0 to this.Y-1 do
            for x = 0 to this.X-1 do
                Vector2I(x, y)
    }
    static member clockwiseCorners looped =
        Corners.clockwise looped Vector2I.Zero Vector2I.Right Vector2I.Down

type Vector2 with
    member this.With () = this
    member this.X_ = Vector2(this.X, 0f)
    member this._Y = Vector2(0f, this.Y)
    member this.YX = Vector2(this.Y, this.X)
    member this.CeilToVector2I () = Vector2I(int ^ ceil this.X, int ^ ceil this.Y)
    member this.ToVector2I () = Vector2I(int this.X, int this.Y)
    static member Half = 0.5f * Vector2.One
    member this.WithHalf = this + Vector2.Half
    static member clockwiseCorners looped =
        Corners.clockwise looped Vector2.Zero Vector2.Right Vector2.Down

type Rect2I with
    member this.With() = this

    static member One = Rect2I(Vector2I.Zero, Vector2I.One)

    member this.AsRect2 = Rect2(
        this.Position.AsVector2
        , this.Size.AsVector2
    )

    member this.Multiply factor = Rect2I(
        this.Position * factor
        , this.Size * factor
    )

    member this.Move shift = Rect2I(
        this.Position + shift
        , this.Size
    )

type Rect2 with
    member this.With() = this

    static member One = Rect2(Vector2.Zero, Vector2.One)

    member this.CeilToRect2I () = Rect2I(
        this.Position.CeilToVector2I()
        , this.Size.CeilToVector2I()
    )

type Corner with
    static member optionsAsSeq (topLeft, topRight, bottomRight, bottomLeft) = 
        seq {
            topLeft, Corner.TopLeft
            topRight, Corner.TopRight
            bottomRight, Corner.BottomRight
            bottomLeft, Corner.BottomLeft
        }
        |> Seq.choose ^ function
            | Some value, side -> Some (value, side)
            | _ -> None

module GetNode =
    type Error =
        | NotExists
        | UnexpectedType of Actual : Node

    let verbose node path =
        NodePath.op_Implicit (path : string)
        |> (node : Node).GetNodeOrNull
        |> function
            | null -> Result.Error NotExists
            | :? 'a as node -> Ok node
            | unexpected -> Result.Error ^ UnexpectedType unexpected

    let safe node path =
        verbose node path
        |> Result.toOption

    exception Exception of Error

    let unsafe node path =
        verbose node path
        |> Result.defaultWith ^ fun err -> raise ^ Exception err

type Node with
    member this.Parent
        with set value = (value : Node).AddChild this
    member this.AddChildren items =
        for item in items do
            this.AddChild item
    member this.Children'AddRange
        with set value =
            this.AddChildren value
    member this.tryGetNode path =
        GetNode.safe this path
    member this.getNode path =
        GetNode.unsafe this path

[<Extension>]
type NodeExt = 
    [<Extension>]
    static member Add'Children<'a, 'item when 'a :> Node and 'item :> Node>(this : 'a, items : 'item seq) : 'a =
        for item in items do
            this.AddChild item
        this
        
    [<Extension>]
    static member Add'Child<'a when 'a :> Node>(this : 'a, child) : 'a = 
        this.AddChild child
        this

    [<Extension>]
    static member With_Process (this : #Node, action) =
        this.AddChild(
            GD.Implements._process ^ action this
        )
        this


type Control with
    member this.Slide
        with set (slide : Vector2I) =
            let topLeft = slide.AsVector2
            let bottomRight = topLeft + Vector2.One
            this.AnchorTop <- topLeft.Y
            this.AnchorBottom <- bottomRight.Y
            this.AnchorLeft <- topLeft.X
            this.AnchorRight <- bottomRight.X
            this.GrowHorizontal <- Control.GrowDirection.Both
            this.GrowVertical <- Control.GrowDirection.Both

    member this.AnchorsPoint
        with set (point : Vector2) =
            this.AnchorTop <- point.Y
            this.AnchorBottom <- point.Y
            this.AnchorLeft <- point.X
            this.AnchorRight <- point.X

    member this.AnchorsRect
        with set (point : Rect2) =
            this.AnchorTop <- point.Position.Y
            this.AnchorBottom <- point.End.Y
            this.AnchorLeft <- point.Position.X
            this.AnchorRight <- point.End.X

type ControlExts =
    [<Extension>]
    static member SetSlide (this : #Control, slide : Vector2I) =
        this.Slide <- slide
        this
    
    [<Extension>]
    static member Set'AnchorsPoint (this : #Control, point) =
        this.AnchorsPoint <- point
        this

    [<Extension>]
    static member Set'AnchorsRect (this : #Control, point) =
        this.AnchorsRect <- point
        this

    [<Extension>]
    static member Centered (this : #Control) =
        this.AnchorsPoint <- Vector2.Half
        this.GrowHorizontal <- Control.GrowDirection.Both
        this.GrowVertical <- Control.GrowDirection.Both
        this

    [<Extension>]
    static member FullRected (this : #Control) =
        this.Set'AnchorsRect(Rect2(Size = Vector2.One))

    [<Extension>]
    static member GrowToCenter (this : #Control) =
        this.GrowHorizontal <-
            if this.AnchorLeft <> this.AnchorRight then
                Control.GrowDirection.Both
            elif this.AnchorLeft = 0f then
                Control.GrowDirection.End
            else
                Control.GrowDirection.Begin
        this.GrowVertical <-
            if this.AnchorTop <> this.AnchorBottom then
                Control.GrowDirection.Both
            elif this.AnchorTop = 0f then
                Control.GrowDirection.End
            else
                Control.GrowDirection.Begin
        this

    [<Extension>]
    static member GrowBoth (this : #Control) =
        this.GrowHorizontal <- Control.GrowDirection.Both
        this.GrowVertical <- Control.GrowDirection.Both
        this

type Resource with
    member this.Disposables
        with set value =
            (value : System.Reactive.Disposables.CompositeDisposable).Add ^ IDisposable.create this.Free

type Node with
    member this.Disposables
        with set value =
            (value : System.Reactive.Disposables.CompositeDisposable).Add ^ IDisposable.create this.QueueFree

type System.IDisposable with
    member this.DisposeSafe () =
        try this.Dispose()
        with ex -> GD.PrintErr (sprintf "%A" ex)

type ResourceExts =
    [<Extension>]
    static member duplicate (this : 'a when 'a :> Resource, ?subresources) =
        this.Duplicate(?subresources = subresources) :?> 'a