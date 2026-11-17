[<AutoOpen>]
module Chapter2.VarExts

open System.Reactive
open FSharp.Control.Reactive
open System.Reactive.Disposables
open Var1

module Var1 =
    type CompositeDisposable with
        member this.ToVarUnsafe observable =
            let mutable state = None
            let get () =
                state
                |> Option.defaultWith ^ fun () ->
                    invalidOp "IObservable was empty!"
            observable
            |> Observable.subscribeObserver { 
                new System.IObserver<'a> with
                    override _.OnNext value =
                        match state with
                        | None ->
                            let local = this.CreateChild()
                            state <- Some {|
                                MVar = local.MVar value
                                Dispose = local.Dispose
                            |}
                        | Some state ->
                            state.MVar.Value <- value
                    override _.OnCompleted () =
                        get().Dispose()
                    override _.OnError exn =
                        get().Dispose()
            }
            |> this.Add
            get().MVar
            |> Observable.Var.paranoid