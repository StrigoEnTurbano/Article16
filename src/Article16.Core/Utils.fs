[<AutoOpen>]
module AutoUtils

let inline (^) f x = f x

let (|Replace|_|) expected value actual =
    if expected = actual then Some value else None

let (|Eq|_|) expected actual =
    if expected = actual then Some () else None

let (|When|_|) condition actual =
    if condition actual then Some () else None

type 'a System.Collections.Generic.IList with
    member this.Last = this.[this.Count - 1]
    member this.IsEmpty = this.Count = 0
    member this.RemoveLast () = this.RemoveAt(this.Count - 1)
    member this.PopLast () =
        let last = this.Last
        this.RemoveLast()
        last

type System.IObservable<'a> with
    member this.subscribe handler =
        this.Subscribe(callback = handler)

module IDisposable =
    let create action = 
        let mutable isDisposed = false
        {
            new System.IDisposable with
                override _.Dispose () =
                    if not isDisposed then
                        isDisposed <- true
                        action ()
        }
    
    let empty = create id
        
    let paranoid disposable =
        create ^ fun () ->
            (disposable : System.IDisposable).Dispose()

    let unite inners =
        create ^ fun () ->
            inners
            |> Seq.iter ^ fun (p : System.IDisposable) ->
                p.Dispose()

    let composite () =
        new System.Reactive.Disposables.CompositeDisposable()