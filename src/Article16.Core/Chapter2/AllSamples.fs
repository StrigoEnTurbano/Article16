module Chapter2.AllSamples

open Godot

open FSharpx.Collections
open FsToolkit.ErrorHandling
open System.Runtime.CompilerServices
open System.Reactive
open FSharp.Control.Reactive
open System.Reactive.Disposables

let samples =
    [
        let valid, invalid =
            let (!) color =
                let image = Image.CreateEmpty(24, 24, false, Image.Format.Rgbaf)
                image.Fill color
                ImageTexture.CreateFromImage image
            !Colors.White.Lerp(Colors.DarkGreen, 0.8f)
            , !Colors.White.Lerp(Colors.DarkRed, 0.8f)
        let inline (!) factory name texture = {|
            Spawn = fun node disposables ->
                GD.whenReady node ^ fun () ->
                    node.AddChild (factory(node, disposables))
            Name = name
            Icon = texture
        |}
        !Chapter1.Sample0.Ready "Спрайтшиты" valid
        !Sample1.Ready "Var" valid
        !Sample2.Ready "ToVarUnsafe" valid
        !Sample3.Ready "Value после смерти" invalid
        !Sample4.Ready "Возвращаем Value" valid
        !Sample5.Ready "Подписка после смерти" invalid
        !Sample6.Ready "Заморозка" valid
        !Sample7.Ready "Ждём смерти" invalid
        !Sample8.Ready "Сообщаем о смерти" valid
        !Sample9.Ready "Property" valid
    ]
    |> List.mapi ^ fun index item -> {|
        item with
            Name = sprintf "%i. %s" index item.Name
    |}

type Ready (main : Node, disposables) =
    inherit Node()
    
    do main.Disposables <- disposables
    
    let itemList = FG.ItemList(
        CustomMinimumSize = Vector2(300f, 300f)
        , FocusMode = Control.FocusModeEnum.None
    )

    let hideHud = {|
        Value = Subject.behavior false
        Key = Key.Tab
    |}
    do  main.AddChild ^ GD.Implements._unhandledKeyInput ^ fun this ev ->
        let ev = ev :?> InputEventKey
        if ev.Keycode = hideHud.Key && not ev.Echo then
            this.GetViewport().SetInputAsHandled()
            hideHud.Value.OnNext ev.Pressed

    let sampleSlot = FG.Node(Parent = main)

    let selected = disposables.Broadcast()
    let items =
        samples
        |> List.map ^ fun item ->
            let restart = disposables.Broadcast()
            let root = FG.Node(Disposables = disposables).Add'Children [
                FG.VBoxContainer(
                    CustomMinimumSize = Vector2.Right * 300f
                ).Add'Children [
                    asNode ^ FG.Label(
                        Text = sprintf "Управление примером:\n%s" item.Name
                        , HorizontalAlignment = HorizontalAlignment.Center
                    )
                    FG.Button(
                        Text = "Перезапустить (F5)"
                        , FocusMode = Control.FocusModeEnum.None
                        , Pressed'Handler = disposables.HandleSignal ^ fun _ () ->
                            restart.Trigger ()
                    )
                    asNode ^ GD.Implements._unhandledKeyInput ^ fun this ev ->
                        let ev = ev :?> InputEventKey
                        if ev.IsJustPressed Key.F5 then
                            this.GetViewport().SetInputAsHandled()
                            restart.Trigger ()
                ]
                |> Margin(10).Wrap
                |> FG.PanelContainer(
                    AnchorsPoint = Vector2.Right
                    , Visible'Bind = disposables.BindMap hideHud.Value not
                ).GrowToCenter().Add'Child
            ]

            let id = itemList.AddItem(item.Name, item.Icon)

            selected.Publish
            |> Observable.map ^ fun p -> p = id
            |> Observable.distinctUntilChanged
            |> Observable.skipWhile not
            |> disposables.Subscribe ^ fun selected ->
                root
                |> if selected then sampleSlot.AddChild else sampleSlot.RemoveChild

            do  let local = disposables.Lap()
                let mutable node = FG.Node(Parent = root)
                do item.Spawn node local.Current

                restart.Publish
                |> disposables.Subscribe ^ fun () ->
                    local.Current.DisposeSafe()
                    local.Complete()
                    node <- FG.Node(Parent = root)
                    item.Spawn node local.Current

            {|
                Name = item.Name
                Root = root
                Restart = restart.Trigger
                Id = id
            |}

    do  itemList.ItemSelected'Handler <- disposables.HandleSignal ^ fun _ id ->
            selected.Trigger ^ int id
        let select id =
            itemList.Select id
            // При установке из кода сигнал не срабатывает.
            selected.Trigger id
        itemList.AddChild ^ GD.Implements._unhandledKeyInput ^ fun this ev ->
            let ev = ev :?> InputEventKey
            if ev.IsFirstPress then
                items
                |> Seq.mapi ^ fun index p ->
                    p
                    , Key.Key0 + LanguagePrimitives.EnumOfValue (int64 index)
                |> Seq.tryFind ^ fun (_, key) ->
                    ev.Keycode = key
                |> Option.iter ^ fun (p, _) ->
                    select p.Id
                    this.GetViewport().SetInputAsHandled()
        select items.Head.Id

    let hud = FG.CanvasLayer(Parent = main)

    do  FG.VBoxContainer().Add'Children [
            asNode ^ FG.Label(
                Text = "Примеры"
                , HorizontalAlignment = HorizontalAlignment.Center
            )
            asNode itemList
        ]
        |> Margin(10).Wrap
        |> FG.PanelContainer(
            Visible'Bind = disposables.BindMap hideHud.Value not
        ).Add'Child
        |> hud.AddChild

    do  FG.VBoxContainer().Add'Children [
            let settings = FG.LabelSettings()
            let (!) str = FG.Label(
                Text = str
                , LabelSettings = settings
            )
            !"- Esc - выход."
            !"- F5 - перезапуск примера"
            !"- 0-9 - выбор примера."
            !(sprintf "- Зажатый(ая) %A скрывает HUD." hideHud.Key)
        ]
        |> Margin(10).Wrap
        |> FG.PanelContainer(
            Visible'Bind = disposables.BindMap hideHud.Value not
            , AnchorsPoint = Vector2.Down
        ).GrowToCenter().Add'Child
        |> hud.AddChild
    
let initWhenReady main =
    GD.whenReady main ^ fun () ->
        main.AddChild (new Ready(main, IDisposable.composite()))