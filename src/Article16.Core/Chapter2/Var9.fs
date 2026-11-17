module Chapter2.Var9

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

    type 'a Property when 'a : equality =
        inherit Var<'a>

    type 'a MProperty when 'a : equality =
        inherit MVar<'a>
        inherit Property<'a>

    module Property =
        let paranoid (property : _ Property) = {
            new Property<'a> with
                override _.Value =
                    property.Value
                override _.Subscribe observer =
                    property.Subscribe observer
        }

type CompositeDisposable with
    member this.Subject () = 
        let inner = Subject.broadcast
        this.Add ^ Disposable.create ^ fun () ->
            if not inner.IsDisposed then
                inner.OnCompleted()
                inner.Dispose()
        { new System.Reactive.Subjects.ISubject<'a> with
            override _.OnCompleted () =
                if not inner.IsDisposed then
                    inner.OnCompleted()
            override _.OnError error =
                if not inner.IsDisposed then
                    inner.OnError error
            override _.OnNext value =
                if not inner.IsDisposed then
                    inner.OnNext value
            override _.Subscribe observer =
                if inner.IsDisposed then
                    observer.OnCompleted()
                    Disposable.empty
                else
                    inner.Subscribe observer
        }

    member this.MVar value =
        let mutable value = value
        let output = this.Subject()
            
        { new Observable.MVar<'a> with
            member _.Value
                with get () = value
                and set newValue =
                    if not this.IsDisposed then
                        value <- newValue
                        output.Trigger newValue
            override _.Subscribe observer =
                observer.Trigger value
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

    member this.MProperty value =
        let mutable value = value
        let output = this.Subject ()
        { new Observable.MProperty<'a> with
            override _.Value
                with get () = value
                and set newValue =
                    if not this.IsDisposed
                        && value <> newValue
                    then
                        value <- newValue
                        output.Trigger newValue
            override _.Subscribe observer =
                observer.Trigger value
                output.Subscribe observer
        }

    member this.ToProperty value observable =
        let property = this.MProperty value
        observable
        |> this.Subscribe ^ fun p -> property.Value <- p
        Observable.Property.paranoid property

    // Вполне сознательно берём Var, а не Property.
    member this.MapProperty mapping var =
        var
        |> Observable.tail
        |> Observable.map mapping
        |> this.ToProperty ^ mapping (var : _ Observable.Var).Value

    member this.ToPropertyUnsafe observable =
        this.createFromFirst (fun p -> p.MProperty) (fun p value -> p.Value <- value) observable
        |> Observable.Property.paranoid