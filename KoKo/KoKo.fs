namespace KoKo

type Image = {
    imageUrl : string
    fileName : string
}

type Rating =
| Safe
| Questionable
| Explicit
| Unknown

type Mipmaps = Image seq

type Post = {
    id : uint64
    fromSpider : ISpider

    rating : Rating
    score : float voption
    sourceUrl : string seq
    tags : string seq
    
    previewImage : Image option
    images : Mipmaps seq
}

and ISpider =
    abstract Name : string
    abstract All : Post seq
    abstract Search : string -> Post seq

module Utils =
    let downloadData (url: string) = async {
        use webClient = new System.Net.WebClient ()
        printfn "DD: %s" url
        try return (Ok <| webClient.DownloadData url)
        with e -> return (Error e)
    }

    let downloadString (url: string) = async {
        use webClient = new System.Net.WebClient ()
        printfn "DS: %s" url
        try return (Ok <| webClient.DownloadString url)
        with e -> return (Error e)
    }

    let getFileNameFromUrl (url: string) =
        let url = url.Replace('\\', '/')
        url.[1 + url.LastIndexOf '/'..] 
        |> System.Web.HttpUtility.UrlDecode

    let normalizeFileName (x: string) = 
        [":";"*";"!";"#";"?";"%";"<";">";"|";"\"";"\\";"/";"\"";"\'"]
        |> List.fold (fun (s: string) (c: string) -> s.Replace (c,"")) x
        |> fun x -> x.Trim()

    let private xmlNormalizer =
        let res = 
            System.Resources.ResourceManager (
                "KoKo.Resources.XMLNormalizer", 
                System.Reflection.Assembly.GetExecutingAssembly())
        let x = res.GetObject("XMLNormalizer") :?> string
        printfn "X:%A" x
        x.Split '\n'
        |> Array.map (fun x -> x.Trim())
        |> Array.map (fun x -> let x = x.Split '\t' in x.[0], x.[1])

    type MixEnumerator<'a> (seqs: 'a seq []) =
        let seqs = seqs |> Array.map (fun x -> x.GetEnumerator())
        let mutable index = -1
        interface System.Collections.Generic.IEnumerator<'a> with
            member this.Current: 'a = lock this (fun () -> seqs.[index].Current)
            member this.Current: obj = lock this (fun () -> seqs.[index].Current :> obj)
            member this.Dispose(): unit = ()

            member x.Reset () = 
                lock x (fun () ->
                    for i in seqs do i.Reset ()
                    index <- -1)

            member x.MoveNext () =
                lock x (fun () ->
                    let mutable brk = false
                    let mutable countDown = seqs.Length + 1
                    let mutable ret = true
                    while not brk do
                        index <- (index + 1) % seqs.Length
                        brk <- seqs.[index].MoveNext ()
                        countDown <- countDown - 1
                        if countDown <= 0 then
                            brk <- true
                            ret <- false
                    ret)

    type MixEnumerable<'a> (seqs: 'a seq []) =
        interface System.Collections.Generic.IEnumerable<'a> with
            member this.GetEnumerator(): System.Collections.IEnumerator = 
                new MixEnumerator<'a> (seqs) :> System.Collections.IEnumerator
            member this.GetEnumerator(): System.Collections.Generic.IEnumerator<'a> =
                new MixEnumerator<'a> (seqs) :> System.Collections.Generic.IEnumerator<'a>


module Image =
    let download (image: Image) = async {
        let mutable retry = 5
        let mutable result : Result<byte[], exn> = Error null
        while retry > 0 do
            try 
                match! Utils.downloadData image.imageUrl with
                | Error e -> raise e
                | Ok x -> 
                    result <- Ok x
                    retry <- 0
            with exn ->
                result <- Error exn
                do! Async.Sleep 10000
            retry <- retry - 1
        return result
    }

module Mipmaps =
    exception NoImageToDownload
    let downloadBestImage (mipmaps: Mipmaps) = async {
        if Seq.tryHead mipmaps |> Option.isSome then
            let data =
                mipmaps
                |> Seq.tryPick (fun image ->
                    Image.download image
                    |> Async.RunSynchronously
                    |> function
                    | Ok x -> Some (x, image)
                    | Error _ -> None)
                |> function
                | None -> 
                    let first = Seq.head mipmaps 
                    let data = Async.RunSynchronously (Image.download first)
                    data |> Result.map (fun x -> x, first)
                | Some x -> Ok x
            return data
        else return (Error NoImageToDownload)
    }

module Spider = 
    let all (spider: #ISpider) = spider.All
    let search tags (spider: #ISpider) = spider.Search tags
    let name (spider: #ISpider) = spider.Name

    let test (spider: #ISpider) =
        try spider.Search "" |> ignore |> Ok
        with e -> Error e
