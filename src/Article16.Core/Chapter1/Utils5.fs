[<AutoOpen>]
module Chapter1.Utils5

open Godot

open FSharpx.Collections
open FsToolkit.ErrorHandling
open System.Runtime.CompilerServices
open System.Reactive
open FSharp.Control.Reactive
open System.Reactive.Disposables

type Range with
    member this.Value'Bind
        with set (binding : _ Binding.Main) =
            binding.ConsumeSetter this.SetValue