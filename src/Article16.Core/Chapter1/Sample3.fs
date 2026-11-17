module Chapter1.Sample3

open Godot

open FSharpx.Collections
open FsToolkit.ErrorHandling
open System.Runtime.CompilerServices
open System.Reactive
open FSharp.Control.Reactive
open System.Reactive.Disposables

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
                    let progress = Observable.scanInit initialProgress (+) deltaSource.Publish
                    let distanceToB =
                        progress
                        |> Observable.map patrolTrack.DistanceToB
                    {|
                        Progress = progress
                        DistanceToB = distanceToB
                        Direction = 
                            distanceToB
                            |> Observable.map ^ fun distance -> if distance > 0f then A2B else B2A
                        Frame =
                            progress
                            |> Observable.map ^ fun p -> props.Lines.Walk.Circledf (10f * p)
                        Position =
                            distanceToB
                            |> Observable.map patrolTrack.PositionOf
                    |}
                let view =
                    FG.Node2D(
                        Disposables = disposables
                        , Parent = center
                        , Position'Bind = disposables.Bind model.Position
                    ).Add'Children [
                        asNode ^ GD.Implements._process ^ fun _ delta ->
                            deltaSource.Trigger ^ float32 delta

                        // `Sprite2D` кладётся в `Node2D`, чтобы его Scale не влиял на других детей.
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

        [
            for unit in [mason;goodspeed] do
                UnitLabel.text unit.Props.Name
                |> unit.View.AddChild
                
                let stylebox = PatrolTrack.stylebox SceneColors.a

                stylebox.BgColor'Bind <- disposables.BindMap unit.Model.Direction ^ function
                    | A2B -> SceneColors.b
                    | B2A -> SceneColors.a

                FG.Label(
                    HorizontalAlignment = HorizontalAlignment.Center
                    , Text'Bind = disposables.BindMap unit.Model.Direction ^ fun p ->
                        match p with
                            | A2B -> "B"
                            | B2A -> "A"
                        |> sprintf "%s идёт к точке %s" unit.Props.Name
                )
                |> Margin(10, 4).Wrap
                |> FG.PanelContainer(Panel = stylebox).Add'Child
                |> asNode
        ]
        |> BottomPanel.create
        |> center.AddChild