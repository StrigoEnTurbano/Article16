
[<AutoOpen>]
module Chapter1.Utils6

open Godot

open FSharpx.Collections
open FsToolkit.ErrorHandling
open System.Runtime.CompilerServices
open System.Reactive
open FSharp.Control.Reactive
open System.Reactive.Disposables

type 'a System.IObserver with
    member this.Trigger ev =
        this.OnNext ev

type 'a System.IObservable with
    // Прячет исходный объект за единственным интерфейсом.
    // По аналогии с `paranoid` функциями в `Hopac`.
    // Имя типа включаем в название, 
    // чтобы не было разночтений в наследниках.
    member this.ParanoidObservable () = {
        new System.IObservable<'a> with
            override _.Subscribe p = this.Subscribe p
    }

type 'a System.Reactive.Subjects.Subject with
    member this.Publish = this.ParanoidObservable ()
            
type CompositeDisposable with
    member this.Register p =
        this.Add p
        p

    member this.CacheObservable observable =
        let output = this.Register Subject.broadcast
        observable
        |> this.Subscribe output.Trigger
        output.Publish