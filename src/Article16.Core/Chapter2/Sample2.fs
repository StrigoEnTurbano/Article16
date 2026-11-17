module Chapter2.Sample2

open Godot

open FSharpx.Collections
open FsToolkit.ErrorHandling
open System.Runtime.CompilerServices
open System.Reactive
open FSharp.Control.Reactive
open System.Reactive.Disposables

open Var1

type Ready (main : Node, disposables) =
    inherit Node()
                
    do main.Disposables <- disposables

    do  let center = FG.Control(Parent = main).Centered()
            
        let patrolTrack = PatrolTrack.Main center

        let goodspeed, mason =
            let rec go () =
                createUnit MiniFolks.Rock.Goodspeed 0f
                , createUnit MiniFolks.Rock.Mason 2f
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
                let view =
                    FG.Node2D(
                        Disposables = disposables
                        , Parent = center
                        , Position'Bind = disposables.Bind model.Position
                    ).Add'Children [
                        asNode ^ GD.Implements._process ^ fun _ delta ->
                            deltaSource.Trigger ^ float32 delta

                        FG.Sprite2D(
                            Name = "Sprite"
                            , Spritesheet = props.Styles.Outlined
                            , TextureFilter = CanvasItem.TextureFilterEnum.Nearest
                            , Scale = Vector2.One * scale
                            , Frame'Bind = disposables.Bind model.Frame
                            , FlipH'Bind = disposables.BindMap model.Direction ^ fun p -> p.IsB2A
                        )
                    ]
                {|
                    Disposables = disposables
                    Props = props
                    Model = model
                    View = view
                |}
            go ()

        for unit in [mason;goodspeed] do
            UnitLabel.button unit.Props.Name ^ disposables.HandleSignal ^ fun _ () ->
                unit.Model.Progress.Value <- 0f
            |> unit.View.AddChild

        [
            asNode ^ FG.HBoxContainer(
                Alignment = BoxContainer.AlignmentMode.Center
            ).Add'Children [
                for unit in [mason;goodspeed] do
                    asNode ^ FG.Button(
                        Text = sprintf "Перезапустить %sа" unit.Props.Name
                        , SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter
                        , FocusMode = Control.FocusModeEnum.None
                        , Pressed'Handler = disposables.HandleSignal ^ fun _ () ->
                            unit.Model.Progress.Value <- 0f
                        , Icon = unit.Props.Icon
                    )
            ]
            asNode ^ Summary.create disposables patrolTrack mason goodspeed
        ]
        |> BottomPanel.create
        |> center.AddChild

and Summary =
    static member create disposables patrolTrack mason goodspeed =
        let rec go () =
            Sample1.Summary.create disposables patrolTrack (f mason) (f goodspeed)
        and f unit = {|
            unit with 
                Model = {|
                    Progress = unit.Model.Progress
                    DistanceToB = unit.Model.DistanceToB
                    Direction = unit.Model.Direction
                    Frame = unit.Model.Frame
                    Position = unit.Model.Position
                |}
        |}
        go ()