[<AutoOpen>]
module RenderingServerDSL

open Godot

[<RequireQualifiedAccess>]
type CanvasId = { AsRid : Rid }

[<RequireQualifiedAccess>]
type TextureId = { AsRid : Rid }

[<RequireQualifiedAccess>]
type ViewportId = { AsRid : Rid }

[<RequireQualifiedAccess>]
type MaterialId = { AsRid : Rid }

[<RequireQualifiedAccess>]
type CanvasItemId = { AsRid : Rid }


type CanvasId with
    member this.Free () = RenderingServer.FreeRid this.AsRid
    member this.AsDisposable () = IDisposable.create this.Free

    static member Wrap rid = { AsRid = rid }
    static member Unwrap id = id.AsRid
    static member Create () = CanvasId.Wrap ^ RenderingServer.CanvasCreate()

type CanvasLayer with
    member this.CanvasId = CanvasId.Wrap ^ this.GetCanvas()

type CanvasItem with
    member this.CanvasId = CanvasId.Wrap ^ this.GetCanvas()

type TextureId with
    member this.Free () = RenderingServer.FreeRid this.AsRid
    member this.AsDisposable () = IDisposable.create this.Free
    
    static member Wrap rid = { AsRid = rid }
    static member Unwrap id = id.AsRid

type Texture with
    member this.TextureId = TextureId.Wrap ^ this.GetRid()

type ViewportId with
    member this.Free () = RenderingServer.FreeRid this.AsRid
    member this.AsDisposable () = IDisposable.create this.Free
    
    static member Wrap rid = { AsRid = rid }
    static member Unwrap id = id.AsRid
    static member Create () = ViewportId.Wrap ^ RenderingServer.ViewportCreate()

type Viewport with
    member this.ViewportId = ViewportId.Wrap ^ this.GetViewportRid()

type MaterialId with   
    member this.Free () = RenderingServer.FreeRid this.AsRid
    member this.AsDisposable () = IDisposable.create this.Free
    
    static member Wrap rid = { AsRid = rid }
    static member Unwrap id = id.AsRid
    static member Create () = MaterialId.Wrap ^ RenderingServer.MaterialCreate()

type Material with
    member this.MaterialId = MaterialId.Wrap ^ this.GetRid()

type CanvasItemId with   
    member this.Free () = RenderingServer.FreeRid this.AsRid
    member this.AsDisposable () = IDisposable.create this.Free
    
    static member Wrap rid = { AsRid = rid }
    static member Unwrap id = id.AsRid
    static member Create () = CanvasItemId.Wrap ^ RenderingServer.CanvasItemCreate()
    
type CanvasItem with
    member this.CanvasItemId = CanvasItemId.Wrap ^ this.GetCanvasItem()


type ViewportId with
    member this.SetActive active = RenderingServer.ViewportSetActive(this.AsRid, active)
    member this.SetTransparentBackground enabled = RenderingServer.ViewportSetTransparentBackground(this.AsRid, enabled)
    member this.SetSize size = RenderingServer.ViewportSetSize(this.AsRid, (size : Vector2I).X, size.Y)

    member this.Active with set value = this.SetActive value
    member this.TransparentBackground with set value = this.SetTransparentBackground value
    member this.Size with set value = this.SetSize value

    member this.AttachCanvas canvasId = RenderingServer.ViewportAttachCanvas(this.AsRid, (canvasId : CanvasId).AsRid)
    member this.GetTexture () =
        RenderingServer.ViewportGetTexture this.AsRid
        |> TextureId.Wrap

type CanvasItemId with
    member this.SetParent parent = RenderingServer.CanvasItemSetParent(this.AsRid, parent.AsRid)
    member this.SetMaterial materialId = RenderingServer.CanvasItemSetMaterial(this.AsRid, (materialId : MaterialId).AsRid)
    member this.SetVisible visible = RenderingServer.CanvasItemSetVisible(this.AsRid, visible)
    member this.SetTransform transform = RenderingServer.CanvasItemSetTransform(this.AsRid, transform)
    member this.SetZIndex zIndex = RenderingServer.CanvasItemSetZIndex(this.AsRid, zIndex)
    member this.SetDefaultTextureFilter filter = RenderingServer.CanvasItemSetDefaultTextureFilter(this.AsRid, filter)

    member this.Parent with set value = this.SetParent value
    member this.Material with set value = this.SetMaterial value
    member this.Visible with set value = this.SetVisible value
    member this.Transform with set value = this.SetTransform value
    member this.ZIndex with set value = this.SetZIndex value
    member this.DefaultTextureFilter with set value = this.SetDefaultTextureFilter value

    member this.Clear () = RenderingServer.CanvasItemClear this.AsRid
    member this.AddSetTransform transform = RenderingServer.CanvasItemAddSetTransform(this.AsRid, transform)

    member this.AddCircle (center, radius, color, ?antialiased) =
        RenderingServer.CanvasItemAddCircle(this.AsRid, center, radius, color, ?antialiased = antialiased)
    member this.AddTextureRect (rect, textureId, ?tile, ?modulate, ?transpose) =
        RenderingServer.CanvasItemAddTextureRect(
            this.AsRid
            , rect
            , (textureId : TextureId).AsRid
            , ?tile = tile
            , ?modulate = modulate
            , ?transpose = transpose
        )
    member this.AddPolyline (points, colors, ?width, ?antialiased) =
        RenderingServer.CanvasItemAddPolyline(this.AsRid, points, colors, ?width = width, ?antialiased = antialiased)
    member this.AddPolygon (points, colors, ?uvs, ?textureId)=
        RenderingServer.CanvasItemAddPolygon(
            this.AsRid
            , points
            , colors
            , ?uvs = uvs
            , ?texture = Option.map TextureId.Unwrap textureId
        )
    member this.AddLine (a, b, color, ?lineWidth, ?antialiased) =
        RenderingServer.CanvasItemAddLine(
            this.AsRid
            , a
            , b
            , color
            , ?width = lineWidth
            , ?antialiased = antialiased
        )
    
    member this.addCircle antialiased center radius color =
        this.AddCircle(center, radius, color, antialiased)

type CanvasId with
    member this.AttachCanvasItem id = RenderingServer.CanvasItemSetParent((id : CanvasItemId).AsRid, this.AsRid)



type CanvasId with
    member this.SetViewport viewport = (viewport : ViewportId).AttachCanvas this
    member this.Viewport with set value = this.SetViewport value

type CanvasItemId with
    member this.SetCanvasParent canvas = (canvas : CanvasId).AttachCanvasItem this
    member this.CanvasParent with set value = this.SetCanvasParent value
    member this.AddCanvasItem canvasItem = (canvasItem : CanvasItemId).SetParent this



type CanvasItemId with
    member this.Disposables
        with set value =
            (value : System.Reactive.Disposables.CompositeDisposable).Add ^ IDisposable.create this.Free