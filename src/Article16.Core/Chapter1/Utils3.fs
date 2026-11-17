[<AutoOpen>]
module Chapter1.Utils3

open Godot

open FSharpx.Collections
open FsToolkit.ErrorHandling
open System.Runtime.CompilerServices
open System.Reactive
open FSharp.Control.Reactive
open System.Reactive.Disposables

type Direction = A2B | B2A

type StyleBoxFlat with
    member this.BgColor'Bind
        with set (binding : _ Binding.Main) =
            binding.ConsumeSetter this.SetBgColor

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