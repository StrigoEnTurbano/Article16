module Chapter1.Sample0

open Godot

open FSharpx.Collections
open FsToolkit.ErrorHandling
open System.Runtime.CompilerServices

type Ready (main : Node, disposables) =
    inherit Node()

    do main.Disposables <- disposables

    do  [
            asNode ^ FG.Label(
                Text = "У нас есть спрайты"
                , HorizontalAlignment = HorizontalAlignment.Center
            )
            FG.GridContainer(
                Columns = 3
            ).Add'Children (
                seq {
                    MiniFolks.Rock.Goodspeed
                    MiniFolks.Rock.Mason
                }
                |> Seq.map ^ fun item -> seq {
                    asNode ^ FG.Label(
                        Text = item.Name
                        , HorizontalAlignment = HorizontalAlignment.Center
                    )
                    let (!) texture = asNode ^ FG.TextureRect(
                        Texture = texture
                        , CustomMinimumSize = texture.GetSize() * 4f
                        , TextureFilter = CanvasItem.TextureFilterEnum.Nearest
                        , ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize
                        , StretchMode = TextureRect.StretchModeEnum.Scale
                    )
                    !item.Styles.Plain.Texture
                    !item.Styles.Outlined.Texture
                }
                |> Seq.append [[
                    asNode ^ FG.Control()
                    FG.Label(
                        Text = "Без контура"
                    )
                    FG.Label(
                        Text = "С контуром"
                    )
                ]]
                |> Seq.transpose
                |> Seq.map ^ Seq.map ^ fun p ->
                    FG.PanelContainer().Add'Child p
                |> Seq.concat
            )
        ]
        |> FG.VBoxContainer().Centered().Add'Children 
        |> main.AddChild