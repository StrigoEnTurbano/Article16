[<AutoOpen>]
module Chapter1.Utils9

open Godot

open FSharpx.Collections
open FsToolkit.ErrorHandling
open System.Runtime.CompilerServices
open System.Reactive
open FSharp.Control.Reactive
open System.Reactive.Disposables

module Observable =
    type 'a CachePrevious (initial, input) =
        let output = Subject.behavior initial
        let sub = Observable.subscribe output.Trigger input

        member this.Previous = output.Value

        interface System.IDisposable with
            override this.Dispose () =
                output.Dispose()
                sub.Dispose()

        interface System.IObservable<'a> with
            override this.Subscribe observer =
                output.Subscribe observer

type CompositeDisposable with
    member this.CachePrevious initial observable =
        new Observable.CachePrevious<_>(initial, observable)
        |> this.Register

    member this.MapCachePrevious mapping (input : _ Observable.CachePrevious) =
        new Observable.CachePrevious<_>(mapping input.Previous, Observable.map mapping input)
        |> this.Register