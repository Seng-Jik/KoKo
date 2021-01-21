namespace KoKo

type Image = {
    width : int
    height : int
    data : Result<byte[],exn> Async
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
        use x = new System.Net.WebClient ()
        try return (Ok <| x.DownloadData url)
        with e -> return (Error e)
    }

    let getFileNameFromUrl (url: string) =
        let url = url.Replace('\\', '/')
        url.[1 + url.LastIndexOf '/'..] 
        |> System.Web.HttpUtility.UrlDecode

module Image =
    let download (image: Image) = async {
        let mutable retry = 5
        let mutable result : Result<byte[], exn> = Error null
        while retry > 0 do
            try 
                match! image.data with
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
                |> Seq.sortByDescending (fun x -> x.width * x.height)
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
    let search (spider: #ISpider) tags = spider.Search tags
    let name (spider: #ISpider) = spider.Name
