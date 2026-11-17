[<AutoOpen>]
module Chapter1.Utils10

open Godot

open FSharpx.Collections
open FsToolkit.ErrorHandling
open System.Runtime.CompilerServices
open System.Reactive
open FSharp.Control.Reactive
open System.Reactive.Disposables

type InputEventKey with
    member this.IsFirstPress =
        not this.Echo && this.Pressed
    member this.IsJustPressed key =
        this.IsFirstPress && this.Keycode = key

type CompositeDisposable with
    member this.Broadcast () =
        Subject.broadcast
        |> this.Register
    member this.Behavior initial =
        Subject.behavior initial
        |> this.Register