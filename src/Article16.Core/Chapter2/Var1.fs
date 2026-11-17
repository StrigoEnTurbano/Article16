module Chapter2.Var1

open System.Reactive
open FSharp.Control.Reactive
open System.Reactive.Disposables

module Observable =
    type 'a Var =
        inherit System.IObservable<'a>
        abstract Value : 'a

    type 'a MVar =
        inherit Var<'a>
        abstract Value : 'a with set

    module Var =
        let paranoid (var : _ Var) = {
            new Var<'a> with
                member _.Value = var.Value
            interface System.IObservable<'a> with
                override _.Subscribe observer =
                    var.Subscribe observer
        }

type CompositeDisposable with
    member this.MVar value =
        let output = this.Behavior value
        { new Observable.MVar<'a> with
            override _.Value
                with get () = output.Value
                and set value = output.OnNext value
            override _.Subscribe observer =
                output.Subscribe observer
        }

    member this.ToVar value observable =
        let mvar = this.MVar value
        observable
        |> this.Subscribe ^ fun p -> mvar.Value <- p
        Observable.Var.paranoid mvar

    member this.MapVar mapping var =
        var
        |> Observable.tail
        |> Observable.map mapping
        |> this.ToVar ^ mapping (var : _ Observable.Var).Value