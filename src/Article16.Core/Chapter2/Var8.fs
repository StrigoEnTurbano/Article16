
module Chapter2.Var8

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
        let mutable value = value
        let output = Subject.broadcast
        this.Add ^ IDisposable.create ^ fun () ->
            output.OnCompleted()
            output.Dispose()
            
        { new Observable.MVar<'a> with
            member _.Value
                with get () = value
                and set newValue =
                    if not output.IsDisposed then
                        value <- newValue
                        output.Trigger newValue
            override _.Subscribe observer =
                observer.Trigger value
                if output.IsDisposed
                then
                    observer.OnCompleted ()
                    Disposable.empty
                else
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
            
    member this.ToVarUnsafe observable =
        this.createFromFirst (fun p -> p.MVar) (fun p value -> p.Value <- value) observable
        |> Observable.Var.paranoid