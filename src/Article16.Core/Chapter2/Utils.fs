[<AutoOpen>]
module Chapter2.Utils

open Godot
open System.Reactive.Disposables
open FSharp.Control.Reactive

// RX

type 'a System.IObserver with
    member this.Trigger ev =
        this.OnNext ev

type 'a System.IObservable with
    // Прячет исходный объект за единственным интерфейсом.
    // По аналогии с `paranoid` функциями в `Hopac`.
    // Имя типа включаем в название, 
    // чтобы не было разночтения в наследниках.
    member this.ParanoidObservable () = {
        new System.IObservable<'a> with
            override _.Subscribe p = this.Subscribe p
    }

type 'a System.Reactive.Subjects.Subject with
    member this.Publish = this.ParanoidObservable ()

type Lap (source : CompositeDisposable) =
    let mutable current = source.CreateChild()

    member this.Current = current
    member this.Complete () =
        if not source.IsDisposed then
            current.Dispose()
            current <- source.CreateChild()

    interface System.IDisposable with
        override this.Dispose () =
            source.Dispose()
        
type CompositeDisposable with
    member this.Register p =
        this.Add p
        p
    member this.Broadcast () =
        Subject.broadcast
        |> this.Register
    member this.Behavior initial =
        Subject.behavior initial
        |> this.Register
    member this.Lap () =
        new Lap (this)

// Godot Binding
        
module Binding =
    type Main<'value> = {
        ConsumeSetter : ('value -> unit) -> unit
    }
        
    let create consumeSetter = {
        ConsumeSetter = consumeSetter
    }
        
type CompositeDisposable with
    member this.Subscribe consume source =
        source
        |> Observable.subscribe consume
        |> this.Add

    member this.Bind source =
        Binding.create ^ fun consume ->
            source
            // Игнорируем повторения.
            |> Observable.distinctUntilChanged
            |> this.Subscribe consume
        
    member this.BindMap source mapping =
        source
        |> Observable.map mapping
        |> this.Bind

// UI : Generic

type PanelContainer with
    member this.Panel
        with set value =
            this.AddThemeStyleboxOverride("panel", value)

type Margin (left, top, right, bottom) =
        
    new (horizontal, vertical) = Margin(horizontal, vertical, horizontal, vertical)
    new (all) = Margin(all, all, all, all)

    member _.Apply container =
        [
            "margin_left", left
            "margin_top", top
            "margin_right", right
            "margin_bottom", bottom
        ]
        |> Seq.iter ^ fun (key, value) ->
            (container : MarginContainer).AddThemeConstantOverride(key, value)

type MarginContainer with
    member this.Margin with set value = (value : Margin).Apply this

type Margin with
    member this.AsContainer () =
        FG.MarginContainer(Margin = this)

    member this.Wrap child =
        this.AsContainer().Add'Child child

type StyleBoxFlat with
    member this.CornerRadius'SetAll
        with set value =
            this.SetCornerRadiusAll value

type CanvasItem with
    member this.Visible'Bind
        with set (binding : _ Binding.Main) =
            binding.ConsumeSetter this.SetVisible

type Sprite2D with
    member this.Frame'Bind
        with set (binding : _ Binding.Main) =
            binding.ConsumeSetter this.SetFrame

    member this.FlipH'Bind
        with set (binding : _ Binding.Main) =
            binding.ConsumeSetter this.SetFlipH

type Node2D with
    member this.Position'Bind
        with set (binding : _ Binding.Main) =
            binding.ConsumeSetter this.SetPosition
            
type Label with
    member this.Text'Bind
        with set (binding : _ Binding.Main) =
            binding.ConsumeSetter this.SetText

type Range with
    member this.Value'Bind
        with set (binding : _ Binding.Main) =
            binding.ConsumeSetter this.SetValue

type StyleBoxFlat with
    member this.BgColor'Bind
        with set (binding : _ Binding.Main) =
            binding.ConsumeSetter this.SetBgColor

type InputEventKey with
    member this.IsFirstPress =
        not this.Echo && this.Pressed
    member this.IsJustPressed key =
        this.IsFirstPress && this.Keycode = key

type BaseButton with
    member this.Enabled'While
        with set value =
            (value : CompositeDisposable).Add ^ IDisposable.create ^ fun () ->
                this.Disabled <- true

// UI : Game

module SceneColors =
    let a = Colors.DarkBlue
    let b = a.With(H = Colors.DarkRed.H)
        
let scale = 10f

module PatrolTrack =
    let stylebox color = FG.StyleBoxFlat(
        BgColor = color
        , CornerRadius'SetAll = 6
    )

    let createMarker label color position = FG.Marker2D(
        Position = position
        , Children'AddRange = [
            FG.Label(
                Text = label
            )
            |> Margin(10, 4).Wrap
            |> FG.PanelContainer(
                Panel = stylebox color
            ).GrowBoth().Add'Child
            |> FG.Node2D(
                Position = Vector2.Down * 20f * scale
            ).Add'Child
        ]
    )

    type Main (parent) =
        let a = createMarker "A" SceneColors.a (Vector2.Left * 32f * scale)
        let b = createMarker "B" SceneColors.b (Vector2.Right * 32f * scale)

        do  a.Parent <- parent
            b.Parent <- parent

        member _.Distance = a.Position.DistanceTo b.Position
        member this.DistanceToB progress =
            let distance = this.Distance
            distance - 200f * progress % (2f * distance)
        member _.PositionOf distanceToB =
            b.Position.MoveToward(a.Position, abs distanceToB)

        member _.A = a
        member _.B = b

module UnitLabel =
    let wrap child =
        child
        |> Margin(10, 4).Wrap
        |> FG.PanelContainer(
            Panel = FG.StyleBoxFlat(
                BgColor = Colors.Black
                , CornerRadius'SetAll = 6
            )
        ).GrowBoth().Add'Child
        |> FG.Node2D(
            Position = Vector2.Up * 4f * scale
        ).Add'Child

    let text name = wrap ^ FG.Label(
        Text = name
    )

    let button name handler = wrap ^ FG.Button(
        Text = name
        , Flat = true
        , FocusMode = Control.FocusModeEnum.None
        , Pressed'Handler = handler
    )

module BottomPanel =
    let create children =
        children
        |> FG.VBoxContainer().Add'Children
        |> Margin(10).Wrap
        |> FG.PanelContainer(
            GrowHorizontal = Control.GrowDirection.Both
            , GrowVertical = Control.GrowDirection.End
            , Panel = FG.StyleBoxFlat(
                BgColor = Colors.Black.Lerp(Colors.White, 0.2f)
                , CornerRadius'SetAll = 6
            )
            , CustomMinimumSize = Vector2.Right * 400f
        ).Add'Child
        |> FG.Node2D(
            Position = Vector2.Down * 24f * scale
        ).Add'Child

type SpritesheetLine = {
    Hframes : int
    Row : int
    FrameCount : int
}
    with

    static member create cols row count = {
        Hframes = cols
        Row = row
        FrameCount = count
    }

    member this.Circled index = this.Row * this.Hframes + index % this.FrameCount
    member inline this.Circledf progress = this.Circled ^ int progress
    member this.OneWay index = this.Circled (min index (this.FrameCount - 1))
    member inline this.OneWayf progress = this.OneWay ^ int progress

type Spritesheet = {
    Texture : Texture2D
    Vframes : int
    Hframes : int
}

type Sprite2D with
    member this.Spritesheet with set value =
        this.Texture <- value.Texture
        this.Vframes <- value.Vframes
        this.Hframes <- value.Hframes
        
let MiniFolks =
    let create name nickname lastWords plain outlined = 
        let inline (!) uid = {
            Texture = GD.load uid
            Vframes = 3
            Hframes = 6
        }
        let styles = {|
            Plain = !plain
            Outlined = !outlined
        |}
        {|
            Name = name
            Nickname = nickname
            LastWords = lastWords
            Styles = styles
            Icon =
                let style = styles.Outlined
                let original = style.Texture.GetImage()
                let size = original.GetSize()
                let size = Vector2I(size.X / style.Hframes / 2, size.Y / style.Vframes / 2)
                use image = Image.CreateEmpty(size.X, size.Y, false, original.GetFormat())
                image.BlitRect(
                    original
                    // Четверть оригинального квадрата по горизонтали.
                    // Половина по вертикали.
                    , Rect2I(Vector2I(size.X / 2, size.Y), size)
                    , Vector2I.Zero
                )
                image.Resize(size.X * 2, size.Y * 2, Image.Interpolation.Nearest)
                ImageTexture.CreateFromImage image
        |}
    let lines = 
        let mutable nextRow = 0
        let (!) count =
            let row = nextRow
            nextRow <- nextRow + 1
            SpritesheetLine.create 6 row count
        {|
            Idle = !4
            Walk = !6
            Death = !5
        |}
    {|
        Rock = {|
            Goodspeed = {|
                create "Гудспид" "Лабораторная крыса" "Это вас развлекает?" "uid://d4afjvspxy5de" "uid://1xaftj1ltjqy" with
                    // Обычно юниты в MiniFolks обладают разным набором анимаций.
                    // То есть проблема не только в различном числе кадров в анимации,
                    // но и в том, что некоторые анимации могут вообще отсутствовать.
                    // По этой причине я могу использовать анонимные рекорды.
                    Lines = lines
            |}
            Mason = {|
                create "Мэйсон" "Шестидесятилетний заключённый" "Лучше бы я стал поэтом." "uid://dy1hpkquldid" "uid://brg3xcgglh8td" with
                    Lines = lines
            |}
        |}
    |}

type Direction = A2B | B2A

    with
    member this.Color =
        match this with
        | A2B -> SceneColors.b
        | B2A -> SceneColors.a

    member this.Destination =
        match this with
        | A2B -> "B"
        | B2A -> "A"

type CompositeDisposable with
    member this.createFromFirst create update observable =
        let mutable state = None
        let get () =
            state
            |> Option.defaultWith ^ fun () ->
                invalidOp "IObservable was empty!"
        observable
        |> Observable.subscribeObserver { 
            new System.IObserver<'a> with
                override _.OnNext value =
                    match state with
                    | None ->
                        let local = this.CreateChild()
                        state <- Some {|
                            Result = create local value
                            Dispose = local.Dispose
                        |}
                    | Some state ->
                        update state.Result value
                override _.OnCompleted () =
                    get().Dispose()
                override _.OnError exn =
                    get().Dispose()
        }
        |> this.Add
        get().Result