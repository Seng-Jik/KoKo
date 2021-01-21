namespace KoKo

type Image = {
    width : int
    height : int
    data : byte[] Async
    fileName : string
    extFileName : string
}

type Rating =
| Safe
| Questionable
| Explicit
| Unknown

type Mipmap = Image seq

type Post = {
    id : uint64
    fromSpider : ISpider

    rating : Rating
    score : float voption
    sourceUrl : string seq
    tags : string seq
    
    previewImage : Image option
    images : Mipmap seq
}

and ISpider =
    abstract Name : string
    abstract All : Post seq
    abstract Search : string list -> Post seq

module Image =
    let download (image: Image) = async {
        let mutable retry = 5
        let mutable result : Result<byte[], exn> = Error null
        while retry > 0 do
            try 
                let! data = image.data
                result <- Ok data
                retry <- 0
            with exn ->
                result <- Error exn
                do! Async.Sleep 10000
            retry <- retry - 1
        return result
    }

module Mipmaps =
    exception NoImageToDownload
    let downloadBestImage (mipmaps: Mipmap) = async {
        if Seq.tryHead mipmaps |> Option.isSome then
            let data =
                mipmaps
                |> Seq.sortByDescending (fun x -> x.width * x.height)
                |> Seq.tryPick (
                    Image.download
                    >> Async.RunSynchronously
                    >> function
                    | Ok x -> Some x
                    | Error _ -> None)
                |> function
                | None -> Async.RunSynchronously (Image.download <| Seq.head post.mipmaps)
                | Some x -> Ok x
            return data
        else return (Error NoImageToDownload)
    }

