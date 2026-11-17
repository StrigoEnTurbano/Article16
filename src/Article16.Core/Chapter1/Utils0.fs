[<AutoOpen>]
module Chapter1.Utils0

open Godot

open FSharpx.Collections
open FsToolkit.ErrorHandling
open System.Runtime.CompilerServices    

type SpritesheetLine = {
    Hframes : int
    Row : int
    FrameCount : int
}
    with

    static member create cols row count = {
        Hframes = cols
        Row = row
        FrameCount = count
    }

    member this.Circled index = this.Row * this.Hframes + index % this.FrameCount
    member inline this.Circledf progress = this.Circled ^ int progress
    member this.OneWay index = this.Circled (min index (this.FrameCount - 1))
    member inline this.OneWayf progress = this.OneWay ^ int progress

type Spritesheet = {
    Texture : Texture2D
    Vframes : int
    Hframes : int
}

type Sprite2D with
    member this.Spritesheet with set value =
        this.Texture <- value.Texture
        this.Vframes <- value.Vframes
        this.Hframes <- value.Hframes

let MiniFolks =
    let create name nickname plain outlined = 
        let inline (!) uid = {
            Texture = GD.load uid
            Vframes = 3
            Hframes = 6
        }
        {|
            Name = name
            Nickname = nickname
            Styles = {|
                Plain = !plain
                Outlined = !outlined
            |}
        |}
    let lines = 
        let mutable nextRow = 0
        let (!) count =
            let row = nextRow
            nextRow <- nextRow + 1
            SpritesheetLine.create 6 row count
        {|
            Idle = !4
            Walk = !6
            Death = !5
        |}
    {|
        Rock = {|
            Goodspeed = {|
                create "Гудспид" "Лабораторная крыса" "uid://d4afjvspxy5de" "uid://1xaftj1ltjqy" with
                    // Обычно юниты в MiniFolks обладают разным набором анимаций.
                    // То есть проблема не только в различном числе кадров в анимации,
                    // но и в том, что некоторые анимации могут вообще отсутствовать.
                    // По этой причине я могу использовать анонимные рекорды.
                    Lines = lines
            |}
            Mason = {|
                create "Мэйсон" "Шестидесятилетний заключённый" "uid://dy1hpkquldid" "uid://brg3xcgglh8td" with
                    Lines = lines
            |}
        |}
    |}