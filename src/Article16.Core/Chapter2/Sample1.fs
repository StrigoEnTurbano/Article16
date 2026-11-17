module Chapter2.Sample1

open Godot

open FSharpx.Collections
open FsToolkit.ErrorHandling
open System.Runtime.CompilerServices
open System.Reactive
open FSharp.Control.Reactive
open System.Reactive.Disposables

open Var1

type Ready (main : Node, disposables) =
    inherit Node()
                
    do main.Disposables <- disposables

    do  let center = FG.Control(Parent = main).Centered()
            
        let patrolTrack = PatrolTrack.Main center

        let goodspeed, mason =
            let rec go () =
                createUnit MiniFolks.Rock.Goodspeed 0f
                , createUnit MiniFolks.Rock.Mason 2f
            and createUnit props initialProgress =
                let disposables = disposables.CreateChild()
                let deltaSource = Event<_>()
                let model = 
                    let progress = disposables.MVar initialProgress
                    deltaSource.Publish
                    |> disposables.Subscribe ^ fun p ->
                        progress.Value <- progress.Value + p

                    let distanceToB =
                        progress
                        |> disposables.MapVar patrolTrack.DistanceToB
                    {|
                        Progress = progress
                        DistanceToB = distanceToB
                        Direction =
                            distanceToB
                            |> Observable.map ^ fun distance -> if distance > 0f then A2B else B2A
                            |> Observable.distinctUntilChanged
                            |> disposables.ToVar A2B
                        Frame =
                            progress
                            |> Observable.map ^ fun p -> props.Lines.Walk.Circledf (10f * p)
                            |> Observable.distinctUntilChanged
                            |> disposables.ToVar ^ props.Lines.Walk.Circledf 0f
                        Position =
                            distanceToB
                            |> disposables.MapVar patrolTrack.PositionOf
                    |}
                let view =
                    FG.Node2D(
                        Disposables = disposables
                        , Parent = center
                        , Position'Bind = disposables.Bind model.Position
                    ).Add'Children [
                        asNode ^ GD.Implements._process ^ fun _ delta ->
                            deltaSource.Trigger ^ float32 delta

                        FG.Sprite2D(
                            Name = "Sprite"
                            , Spritesheet = props.Styles.Outlined
                            , TextureFilter = CanvasItem.TextureFilterEnum.Nearest
                            , Scale = Vector2.One * scale
                            , Frame'Bind = disposables.Bind model.Frame
                            , FlipH'Bind = disposables.BindMap model.Direction ^ fun p -> p.IsB2A
                        )
                    ]
                {|
                    Disposables = disposables
                    Props = props
                    Model = model
                    View = view
                |}
            go ()

        for unit in [mason;goodspeed] do
            UnitLabel.button unit.Props.Name ^ disposables.HandleSignal ^ fun _ () ->
                unit.Model.Progress.Value <- 0f
            |> unit.View.AddChild

        [
            asNode ^ FG.HBoxContainer(
                Alignment = BoxContainer.AlignmentMode.Center
            ).Add'Children [
                for unit in [mason;goodspeed] do
                    asNode ^ FG.Button(
                        Text = sprintf "Перезапустить %sа" unit.Props.Name
                        , SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter
                        , FocusMode = Control.FocusModeEnum.None
                        , Pressed'Handler = disposables.HandleSignal ^ fun _ () ->
                            unit.Model.Progress.Value <- 0f
                        , Icon = unit.Props.Icon
                    )
            ]
            asNode ^ Summary.create disposables patrolTrack mason goodspeed
        ]
        |> BottomPanel.create
        |> center.AddChild
        
and Summary =
    static member create (disposables : CompositeDisposable) patrolTrack mason goodspeed =
        [
            FG.GridContainer(
                Columns = 2
            ).Add'Children [
                yield! [
                    let (!) text = asNode ^ FG.Label(
                        Text = text
                        , HorizontalAlignment = HorizontalAlignment.Center
                        , VerticalAlignment = VerticalAlignment.Bottom
                        , SizeFlagsVertical = Control.SizeFlags.Fill
                    )
                    !"Спрайт"
                    !"Модель"
                ]
            
                for unit in [mason;goodspeed] do
                    let spawnSprite children =
                        children
                        |> FG.Control().Centered().Add'Children
                        |> FG.ColorRect(
                            Color = Colors.Transparent
                            , CustomMinimumSize = Vector2.One * 64f
                        ).Add'Child
            
                    let localSprite = FG.Sprite2D(
                        Spritesheet = unit.Props.Styles.Outlined
                        , Scale = Vector2.One * 2f
                        , TextureFilter = CanvasItem.TextureFilterEnum.Nearest
                        , FlipH'Bind = disposables.BindMap unit.Model.Direction ^ fun p -> p.IsB2A
                        , Frame'Bind = disposables.Bind unit.Model.Frame
                    )
                    spawnSprite [localSprite]
            
                    FG.VBoxContainer(
                        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
                        , Alignment = BoxContainer.AlignmentMode.End
                    ).Add'Children [
                        asNode ^ FG.Label(
                            Text'Bind = disposables.BindMap unit.Model.Progress ^ sprintf "Прогресс: %0.3f"
                            , HorizontalAlignment = HorizontalAlignment.Center
                        )
            
                        asNode ^ FG.HSlider(
                            Editable = false
                            , MaxValue = float patrolTrack.Distance
                            , Value'Bind = disposables.BindMap unit.Model.DistanceToB ^ fun toB ->
                                float (patrolTrack.Distance - abs toB)
                        )
            
                        let stylebox = PatrolTrack.stylebox SceneColors.a
                                            
                        stylebox.BgColor'Bind <- disposables.BindMap unit.Model.Direction ^ fun p -> 
                            p.Color
            
                        FG.Label(
                            HorizontalAlignment = HorizontalAlignment.Center
                            , Text'Bind = disposables.BindMap unit.Model.Direction ^ fun p ->
                                p.Destination
                                |> sprintf "%s идёт к точке %s" unit.Props.Name
                        )
                        |> Margin(10, 4).Wrap
                        |> FG.PanelContainer(Panel = stylebox).Add'Child
                        |> asNode
                    ]
            ]
        ]
        |> FG.VBoxContainer(
            Disposables = disposables
        ).Add'Children