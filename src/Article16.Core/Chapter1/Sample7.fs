module Chapter1.Sample7

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
                    let progress = 
                        Observable.scanInit initialProgress (+) deltaSource.Publish
                        |> disposables.CacheObservable
                    let distanceToB =
                        progress
                        |> Observable.map patrolTrack.DistanceToB
                        |> disposables.CacheObservable
                    {|
                        Progress = progress
                        DistanceToB = distanceToB
                        Direction = 
                            distanceToB
                            |> Observable.map ^ fun distance -> if distance > 0f then A2B else B2A
                            |> disposables.CacheObservable
                        Frame =
                            progress
                            |> Observable.map ^ fun p -> props.Lines.Walk.Circledf (10f * p)
                            |> disposables.CacheObservable
                        Position =
                            distanceToB
                            |> Observable.map patrolTrack.PositionOf
                            |> disposables.CacheObservable
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
            UnitLabel.text unit.Props.Name
            |> unit.View.AddChild

        Sample5.Summary.create disposables patrolTrack mason goodspeed
        |> BottomPanel.create
        |> center.AddChild