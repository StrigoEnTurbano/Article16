module Article16.Core.EntryPoint

open Godot
open FsToolkit.ErrorHandling

let summonFSharp node =
    match string (node : Node).Name with
    | "EntryPoint" ->
        node.AddChild ^ GD.Implements._unhandledKeyInput ^ fun this ev ->
            let ev = ev :?> InputEventKey
            if ev.Keycode = Key.Escape && ev.Pressed && not ev.Echo then
                this.GetTree().Free()
    | "CmdlineUserArgs" ->
        OS.GetCmdlineUserArgs()
        |> List.ofSeq
        |> function
            | "Chapter1" :: "All" :: [] ->
                Chapter1.AllSamples.initWhenReady node
            | []
            | "Chapter2" :: "All" :: [] ->
                Chapter2.AllSamples.initWhenReady node
            | Replace "Chapter1" Chapter1.AllSamples.samples samples :: sample :: []
            | Replace "Chapter2" Chapter2.AllSamples.samples samples :: sample :: [] ->
                System.Int32.TryParse sample
                |> Option.ofPair
                |> Option.bind ^ fun id ->
                    Chapter1.AllSamples.samples
                    |> List.tryItem id
                |> function
                    | Some item ->
                        item.Spawn node (IDisposable.composite())
                    | None ->
                        GD.whenReady node ^ fun () ->
                            node.AddChild ^ FG.Label(
                                Text = sprintf "Unexpected sample id: %A" sample
                            )
            | other ->
                GD.whenReady node ^ fun () ->
                    node.AddChild ^ FG.Label(
                        Text = sprintf "Unexpected cmdline user args: %A" other
                    )
    | unexpected ->
        GD.print $"unexpected node.Name: %A{unexpected}"