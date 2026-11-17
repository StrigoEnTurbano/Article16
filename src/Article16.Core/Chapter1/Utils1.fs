[<AutoOpen>]
module Chapter1.Utils1

open Godot

open FSharpx.Collections
open FsToolkit.ErrorHandling
open System.Runtime.CompilerServices
open System.Reactive
open FSharp.Control.Reactive

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