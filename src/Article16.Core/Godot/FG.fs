namespace Godot

type FG =
    static member ignore (node : #Node) = ()
    
    static member Node () =
        new Node()

    static member Node2D () =
        new Node2D()

    static member Control () =
        new Control()

    static member Camera2D () =
        new Camera2D()

    static member Label () =
        new Label()

    static member CanvasLayer () =
        new CanvasLayer()

    static member ColorRect () =
        new ColorRect()

    static member PanelContainer () =
        new PanelContainer()

    static member MarginContainer () =
        new MarginContainer()

    static member VBoxContainer () =
        new VBoxContainer()

    static member HBoxContainer () =
        new HBoxContainer()

    static member Button () =
        new Button()

    static member ButtonGroup () =
        new ButtonGroup()

    static member CheckButton () =
        new CheckButton()

    static member ProgressBar () =
        new ProgressBar()

    static member TextureRect () =
        new TextureRect()

    static member LabelSettings () =
        new LabelSettings()

    static member StyleBoxFlat () =
        new StyleBoxFlat()
        
    static member StyleBoxEmpty () =
        new StyleBoxEmpty()

    static member ConfirmationDialog () =
        new ConfirmationDialog()

    static member GridContainer () =
        new GridContainer()

    static member Sprite2D () =
        new Sprite2D()

    static member Marker2D () =
        new Marker2D()

    static member HSlider () =
        new HSlider()
        
    static member VSlider () =
        new VSlider()

    static member ItemList () =
        new ItemList()

    static member AtlasTexture () =
        new AtlasTexture()
    
    static member ImageTexture () =
        new ImageTexture()