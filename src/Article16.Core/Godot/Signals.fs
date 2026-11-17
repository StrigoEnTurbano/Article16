[<AutoOpen>]
module Signals

open Godot
open System.Reactive.Disposables

module SignalHandler =
    type Main<'node, 'message> = {
        Trigger : 'node -> 'message -> unit
        ConsumeSub : System.IDisposable -> unit
    }

    let create consumeSub trigger = {
        Trigger = trigger
        ConsumeSub = consumeSub
    }
    
type CompositeDisposable with
    member this.CreateChild () =
        let child = IDisposable.composite ()
        this.Add child
        child.Add ^ IDisposable.create ^ fun () -> ignore ^ this.Remove child
        child

    member this.HandleSignal trigger =
        SignalHandler.create this.Add trigger 

    member this.HandleSignalOneTime trigger =
        let child = this.CreateChild ()
        SignalHandler.create child.Add ^ fun a b ->
            trigger a b
            child.Dispose()

module IDisposable =
    let handleSignalOneTime trigger =
        let child = IDisposable.composite ()
        SignalHandler.create child.Add ^ fun node message ->
            trigger node message
            child.Dispose()

type Node with
    member this.Ready'Handler
        with set (value : SignalHandler.Main<_,_>) =
            let handler = System.Action(fun () -> value.Trigger this ())
            this.add_Ready handler
            value.ConsumeSub ^ IDisposable.create ^ fun () -> this.remove_Ready handler

type CanvasItem with
    member this.Draw'Handler
        with set (value : SignalHandler.Main<_,_>) =
            let handler = System.Action(fun () -> value.Trigger this ())
            this.add_Draw handler
            value.ConsumeSub ^ IDisposable.create ^ fun () -> this.remove_Draw handler

type Control with
    member this.GuiInput'Handler
        with set (value : SignalHandler.Main<_,_>) =
            let handler = Control.GuiInputEventHandler(fun ev -> value.Trigger this ev)
            this.add_GuiInput handler
            value.ConsumeSub ^ IDisposable.create ^ fun () -> this.remove_GuiInput handler

    member this.ItemRectChanged'Handler
        with set (value : SignalHandler.Main<_,_>) =
            let handler = System.Action(fun () -> value.Trigger this ())
            this.add_ItemRectChanged handler
            value.ConsumeSub ^ IDisposable.create ^ fun () -> this.remove_ItemRectChanged handler
            
    member this.MouseEntered'Handler
        with set (value : SignalHandler.Main<_,_>) =
            let handler = System.Action(fun () -> value.Trigger this ())
            this.add_MouseEntered handler
            value.ConsumeSub ^ IDisposable.create ^ fun () -> this.remove_MouseEntered handler
            
    member this.MouseExited'Handler
        with set (value : SignalHandler.Main<_,_>) =
            let handler = System.Action(fun () -> value.Trigger this ())
            this.add_MouseExited handler
            value.ConsumeSub ^ IDisposable.create ^ fun () -> this.remove_MouseExited handler

type BaseButton with
    member this.Pressed'Handler
        with set (value : SignalHandler.Main<_,_>) =
            let handler = System.Action(fun () -> value.Trigger this ())
            this.add_Pressed handler
            value.ConsumeSub ^ IDisposable.create ^ fun () -> this.remove_Pressed handler
    
    member this.ButtonDown'Handler
        with set (value : SignalHandler.Main<_,_>) =
            let handler = System.Action(fun () -> value.Trigger this ())
            this.add_ButtonDown handler
            value.ConsumeSub ^ IDisposable.create ^ fun () -> this.remove_ButtonDown handler

    member this.ButtonUp'Handler
        with set (value : SignalHandler.Main<_,_>) =
            let handler = System.Action(fun () -> value.Trigger this ())
            this.add_ButtonUp handler
            value.ConsumeSub ^ IDisposable.create ^ fun () -> this.remove_ButtonUp handler

type ConfirmationDialog with
    member this.Confirmed'Handler
        with set (value : SignalHandler.Main<_,_>) =
            let handler = System.Action(fun () -> value.Trigger this ())
            this.add_Confirmed handler
            value.ConsumeSub ^ IDisposable.create ^ fun () -> this.remove_Confirmed handler

    member this.Canceled'Handler
        with set (value : SignalHandler.Main<_,_>) =
            let handler = System.Action(fun () -> value.Trigger this ())
            this.add_Canceled handler
            value.ConsumeSub ^ IDisposable.create ^ fun () -> this.remove_Canceled handler

type ItemList with
    member this.ItemSelected'Handler
        with set (value : SignalHandler.Main<_,_>) =
            let handler = ItemList.ItemSelectedEventHandler(fun id -> value.Trigger this id)
            this.add_ItemSelected handler
            value.ConsumeSub ^ IDisposable.create ^ fun () -> this.remove_ItemSelected handler