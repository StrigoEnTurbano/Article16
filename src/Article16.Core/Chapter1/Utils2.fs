[<AutoOpen>]
module Chapter1.Utils2

open Godot

open FSharpx.Collections
open FsToolkit.ErrorHandling
open System.Runtime.CompilerServices
open System.Reactive
open FSharp.Control.Reactive
open System.Reactive.Disposables

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