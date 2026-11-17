module Chapter1.Sample5

open Godot

open FSharpx.Collections
open FsToolkit.ErrorHandling
open System.Runtime.CompilerServices
open System.Reactive
open FSharp.Control.Reactive
open System.Reactive.Disposables

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
                    let progress = Observable.scanInit initialProgress (+) deltaSource.Publish
                    let distanceToB =
                        progress
                        |> Observable.map patrolTrack.DistanceToB
                    {|
                        Progress = progress
                        DistanceToB = distanceToB
                        Direction = 
                            distanceToB
                            |> Observable.map ^ fun distance -> if distance > 0f then A2B else B2A
                        Frame =
                            progress
                            |> Observable.map ^ fun p -> props.Lines.Walk.Circledf (10f * p)
                        Position =
                            distanceToB
                            |> Observable.map patrolTrack.PositionOf
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
            UnitLabel.text unit.Props.Name
            |> unit.View.AddChild
            
        Summary.create disposables patrolTrack mason goodspeed
        |> BottomPanel.create
        |> center.AddChild

and Summary =
    static member create (disposables : CompositeDisposable) patrolTrack mason goodspeed = [
        let visibilityChange = Event<_>()
            
        asNode ^ FG.CheckButton(
            Text = "Показывать сводку"
            , Pressed'Handler = disposables.HandleSignal ^ fun btn () ->
                visibilityChange.Trigger btn.ButtonPressed
            , FocusMode = Control.FocusModeEnum.None
        )
        asNode ^ FG.ColorRect(
            CustomMinimumSize = Vector2.Down * 4f
            , Color = Colors.White.With(A = 0.2f)
        )
        let slot = Margin(0).AsContainer()
        asNode slot
        let spawnStub disposables = slot.AddChild ^ FG.Label(
            Text = "Сводка скрыта"
            , HorizontalAlignment = HorizontalAlignment.Center
            , Disposables = disposables
        )
        let mutable local = disposables.CreateChild()
        spawnStub local
        visibilityChange.Publish
        |> disposables.Subscribe ^ fun isVisible ->
            local.Dispose()
            local <- disposables.CreateChild()
            if not isVisible then spawnStub local else
            let disposables = local
            [
                FG.GridContainer(
                    Columns = 3
                ).Add'Children [
                    yield! [
                        let (!) text = asNode ^ FG.Label(
                            Text = text
                            , HorizontalAlignment = HorizontalAlignment.Center
                            , VerticalAlignment = VerticalAlignment.Bottom
                            , SizeFlagsVertical = Control.SizeFlags.Fill
                        )
            
                        !"Спрайт:\n В примере "
                        !"Спрайт:\n В сводке "
                        !"Модель:\nВ сводке"
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
                        let actual = spawnSprite [localSprite]
            
                        spawnSprite [
                            let sprite = FG.Sprite2D(
                                Spritesheet = unit.Props.Styles.Outlined
                                , Scale = Vector2.One * 2f
                                , TextureFilter = CanvasItem.TextureFilterEnum.Nearest
                            )
                            asNode sprite
                            let superLocal = local.CreateChild()
                            unit.Disposables.Add superLocal
                            if not superLocal.IsDisposed then
                                let source : Sprite2D = unit.View.getNode "Sprite"
                                asNode ^ GD.Implements._process(
                                    fun _ _ ->
                                        sprite.Frame <- source.Frame
                                        sprite.FlipH <- source.FlipH
                                        actual.Color <-
                                            if sprite.FlipH <> localSprite.FlipH
                                            then Colors.Red
                                            elif sprite.Frame <> localSprite.Frame
                                            then Colors.Yellow
                                            else Colors.Green
                                    , Disposables = superLocal
                                )
                            superLocal.Add ^ IDisposable.create ^ fun () ->
                                actual.Color <- Colors.Violet
                        ]
            
                        actual
            
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
                                            
                            stylebox.BgColor'Bind <- disposables.BindMap unit.Model.Direction ^ function
                                | A2B -> SceneColors.b
                                | B2A -> SceneColors.a
            
                            FG.Label(
                                HorizontalAlignment = HorizontalAlignment.Center
                                , Text'Bind = disposables.BindMap unit.Model.Direction ^ fun p ->
                                    match p with
                                        | A2B -> "B"
                                        | B2A -> "A"
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
            |> slot.AddChild 
    ]