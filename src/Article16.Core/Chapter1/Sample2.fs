module Chapter1.Sample2

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

        do  let rec go () =
                spawnUnit MiniFolks.Rock.Goodspeed 0f
                spawnUnit MiniFolks.Rock.Mason 2f
            and spawnUnit unit initialProgress =
                let disposables = disposables.CreateChild()
                let deltaSource = Event<_>()
                let progress = Observable.scanInit initialProgress (+) deltaSource.Publish
                let distanceToB =
                    progress
                    |> Observable.map patrolTrack.DistanceToB
                center.AddChild ^ FG.Sprite2D(
                    Disposables = disposables
                    , Position'Bind = disposables.BindMap distanceToB patrolTrack.PositionOf
                    , Spritesheet = unit.Styles.Outlined
                    , TextureFilter = CanvasItem.TextureFilterEnum.Nearest
                    , Scale = Vector2.One * scale
                    , Children'AddRange = [
                        GD.Implements._process ^ fun this delta ->
                            deltaSource.Trigger ^ float32 delta
                    ]
                    , Frame'Bind = disposables.BindMap progress ^ fun p ->
                        unit.Lines.Walk.Circledf (10f * p)
                    , FlipH'Bind = disposables.BindMap distanceToB ^ fun p ->
                        p < 0f
                )
            go ()