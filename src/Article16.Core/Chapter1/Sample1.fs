module Chapter1.Sample1

open Godot

open FSharpx.Collections
open FsToolkit.ErrorHandling
open System.Runtime.CompilerServices
open System.Reactive
open FSharp.Control.Reactive

type Ready (main : Node, disposables) =
    inherit Node()

    do main.Disposables <- disposables

    do  let center = FG.Control(Parent = main).Centered()

        let deltaSource = Event<_>()
            
        let sprite = FG.Sprite2D(
            Parent = center
            , Spritesheet = MiniFolks.Rock.Mason.Styles.Outlined
            , Scale = Vector2.One * 10f
            , Children'AddRange = [
                GD.Implements._process ^ fun _ delta ->
                    deltaSource.Trigger ^ float32 delta
            ]
            , TextureFilter = CanvasItem.TextureFilterEnum.Nearest
        )

        let track = PatrolTrack.Main center

        deltaSource.Publish
        // Эта штука просуммирует `delta` и текущий `state`
        |> Observable.scanInit 0f (+)
        |> Observable.subscribe ^ fun progress ->
            sprite.Frame <- MiniFolks.Rock.Mason.Lines.Walk.Circledf (10f * progress)
            let distanceToB = track.DistanceToB progress
            sprite.Position <- track.PositionOf distanceToB
            sprite.FlipH <- distanceToB < 0f
        |> disposables.Add