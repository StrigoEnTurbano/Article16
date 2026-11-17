module Chapter2.Sample5

open Godot

open FSharpx.Collections
open FsToolkit.ErrorHandling
open System.Runtime.CompilerServices
open System.Reactive
open FSharp.Control.Reactive
open System.Reactive.Disposables

open Var4

type Unit (core) =
    member val Core = core

type Ready (main : Node, disposables) =
    inherit Node()
                
    do main.Disposables <- disposables

    do  let center = FG.Control(Parent = main).Centered()
            
        let patrolTrack = PatrolTrack.Main center

        let goodspeed, mason, makeView =
            let rec go () =
                createUnit MiniFolks.Rock.Goodspeed 0f
                , createUnit MiniFolks.Rock.Mason 2f
                , makeView
            and createUnit props initialProgress =
                let disposables = disposables.CreateChild()
                let deltaSource = Event<_>()
                let model = 
                    let progress = disposables.MVar initialProgress
                    deltaSource.Publish
                    |> disposables.Subscribe ^ fun p ->
                        progress.Value <- progress.Value + p

                    let distanceToB =
                        progress
                        |> disposables.MapVar patrolTrack.DistanceToB
                    {|
                        Progress = progress
                        DistanceToB = distanceToB
                        Direction =
                            distanceToB
                            |> Observable.map ^ fun distance -> if distance > 0f then A2B else B2A
                            |> Observable.distinctUntilChanged
                            |> disposables.ToVarUnsafe
                        Frame =
                            progress
                            |> Observable.map ^ fun p -> props.Lines.Walk.Circledf (10f * p)
                            |> Observable.distinctUntilChanged
                            |> disposables.ToVarUnsafe
                        Position =
                            distanceToB
                            |> disposables.MapVar patrolTrack.PositionOf
                    |}
                let view : Node2D = makeView disposables props model
                GD.Implements._process(fun _ delta -> deltaSource.Trigger ^ float32 delta).Parent <- view
                Unit {|
                    Disposables = disposables
                    Props = props
                    Model = model
                    View = view
                |}
            and makeView disposables props model =
                FG.Node2D(
                    Disposables = disposables
                    , Parent = center
                    , Position'Bind = disposables.Bind model.Position
                ).Add'Children [
                    FG.Sprite2D(
                        Name = "Sprite"
                        , Spritesheet = props.Styles.Outlined
                        , TextureFilter = CanvasItem.TextureFilterEnum.Nearest
                        , Scale = Vector2.One * scale
                        , Frame'Bind = disposables.Bind model.Frame
                        , FlipH'Bind = disposables.BindMap model.Direction ^ fun p -> p.IsB2A
                    )
                ]
            go ()
            
        let messages = 
            let view = FG.VBoxContainer()
            {|
                View = view
                Add = fun color text -> view.AddChild ^ FG.Label(
                    Text = text
                    , LabelSettings = FG.LabelSettings(
                        FontColor = color
                    )
                )
            |}
            
        let killUnit (unit : Unit) =
            let unit = unit.Core
            unit.Disposables.Dispose()
            do  try unit.Model.Direction.Value.Destination
                    |> sprintf "%s убит в %0.3f, когда шёл в точку %s." unit.Props.Name unit.Model.Progress.Value
                    |> messages.Add Colors.Green
                with
                | ex ->
                    sprintf "Ошибка при чтении из %sа:\n%s" unit.Props.Name ex.Message
                    |> messages.Add Colors.Red
            try let view = makeView disposables unit.Props unit.Model
                view.Modulate <- Colors.Blue.With(A = 0.6f) + Colors.White * 0.2f
                view.AddChild ^ UnitLabel.text unit.Props.LastWords
            with ex ->
                messages.Add Colors.Red ^ sprintf "Ошибка при спавне призрака %s:\n%s" unit.Props.Name ex.Message

        for unit in [mason;goodspeed] do
            let core = unit.Core
            UnitLabel.button core.Props.Name ^ disposables.HandleSignal ^ fun _ () ->
                killUnit unit
            |> core.View.AddChild

        [
            asNode ^ FG.Label(
                Text = "Сопровождается выбросом исключения.\nВ IDE двигайтесь дальше."
                , HorizontalAlignment = HorizontalAlignment.Center
                , LabelSettings = FG.LabelSettings(
                    FontSize = 8
                )
            )
            asNode ^ FG.HBoxContainer(
                Alignment = BoxContainer.AlignmentMode.Center
            ).Add'Children [
                for unit in [mason;goodspeed] do
                    let core = unit.Core
                    FG.Button(
                        Text = sprintf "Убить %sа" core.Props.Name
                        , FocusMode = Control.FocusModeEnum.None
                        , Pressed'Handler = disposables.HandleSignal ^ fun _ () ->
                            killUnit unit
                        , Icon = core.Props.Icon
                        , Enabled'While = core.Disposables
                    )
            ]
            messages.View
        ]
        |> BottomPanel.create
        |> center.AddChild