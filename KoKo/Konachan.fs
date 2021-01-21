module KoKo.Konachan

open FSharp.Data

type RequestFormat = Printf.StringFormat<string->int->string->string>
type SourceUrlFormat = Printf.StringFormat<string->uint64->string>

type SpiderArguments = {
    name : string
    domain : string
    requestFormat : RequestFormat
    sourceUrlFormat : SourceUrlFormat
    sourceUrlDomain : string option
}

module RequestFormats =
    let Konachan : RequestFormat = "%s/post.xml?limit=100&page=%d&tags=%s"
    let HypnoHub : RequestFormat = "%s/post/index.xml?limit=100&page=%d&tags=%s"
    let Gelbooru : RequestFormat = "%s/index.php?page=dapi&s=post&q=index&&limit=100&pid=%d&tags=%s"

module SourceUrlFormats =
    let Konachan : SourceUrlFormat = "%s/post/show/%u"
    let Gelbooru : SourceUrlFormat = "%s/index.php?page=post&s=view&id=%u"

let parseRating = function
| "s" -> Safe
| "q" -> Questionable
| "e" -> Explicit
| _ -> Unknown


type PostParser = XmlProvider<"https://konachan.net/post.xml?page=1&limit=100">

type KonachanSpider (args: SpiderArguments) =
    interface ISpider with
        member _.Name = args.name
        member x.All = (x :> ISpider).Search ""
        member spider.Search tags = 
            let pages =
                Seq.initInfinite (fun pageId ->
                    let mutable retry = 5
                    let mutable result : Result<PostParser.Posts, exn> = Error null
                    while retry > 0 do
                        try
                            sprintf args.requestFormat args.domain pageId tags
                            |> Utils.downloadString
                            |> Async.RunSynchronously
                            |> function
                            | Error e -> raise e
                            | Ok x -> x |> PostParser.Parse
                            |> fun xml -> result <- Ok xml
                            retry <- 0
                        with e -> 
                            result <- Error e
                            retry <- retry - 1
                    match result with
                    | Ok x -> x 
                    | Error e -> raise e)
            let count = (Seq.head pages).Count
            pages
            |> Seq.collect (fun x -> x.Posts)
            |> Seq.take count
            |> Seq.map (fun x -> {      // TODO: Wrap here to a 'try' block.
                id = uint64 x.Id
                fromSpider = spider
                rating = parseRating x.Rating
                    
                score = x.Score |> float |> ValueSome
                sourceUrl = seq { 
                    sprintf args.sourceUrlFormat (args.sourceUrlDomain |> Option.defaultValue args.domain) <| uint64 x.Id
                    if x.Source.IsSome then 
                        if not <| System.String.IsNullOrWhiteSpace x.Source.Value then
                            x.Source.Value 
                }
                tags = x.Tags.Trim().Split ' '
                previewImage = 
                    Some {
                        imageUrl = x.PreviewUrl
                        fileName = Utils.getFileNameFromUrl x.PreviewUrl
                    }

                images = seq {
                    seq {
                        {
                            imageUrl = x.FileUrl
                            fileName = Utils.getFileNameFromUrl x.FileUrl
                        }

                        let attrs = x.XElement.Attributes()
                        if attrs |> Seq.exists (fun x -> x.Name.LocalName = "jpeg_url") then
                            {
                                imageUrl = x.JpegUrl
                                fileName = Utils.getFileNameFromUrl x.JpegUrl
                            }

                        if attrs |> Seq.exists (fun x -> x.Name.LocalName = "sample_url") then
                            {
                                imageUrl = x.SampleUrl
                                fileName = Utils.getFileNameFromUrl x.SampleUrl
                            }
                    }
                }
            })

let Konachan = KonachanSpider { 
    name = "Konachan"
    domain = "http://konachan.net" 
    requestFormat = RequestFormats.Konachan
    sourceUrlFormat = SourceUrlFormats.Konachan
    sourceUrlDomain = Some "http://konachan.com"
}

let Lolibooru = KonachanSpider {
    name = "Lolibooru"
    domain = "https://lolibooru.moe"
    requestFormat = RequestFormats.Konachan
    sourceUrlFormat = SourceUrlFormats.Konachan
    sourceUrlDomain = None
}

let Gelbooru = KonachanSpider {
    name = "Gelbooru"
    domain = "https://gelbooru.com"
    requestFormat = RequestFormats.Gelbooru
    sourceUrlFormat = SourceUrlFormats.Gelbooru
    sourceUrlDomain = None
}

let Yandere = KonachanSpider {
    name = "Yandere"
    domain = "https://yande.re"
    requestFormat = RequestFormats.Konachan
    sourceUrlFormat = SourceUrlFormats.Konachan
    sourceUrlDomain = None
}

let TheBigImageBoard = KonachanSpider {
    name = "The Big ImageBoard (TBIB)"
    domain = "https://tbib.org"
    requestFormat = RequestFormats.Gelbooru
    sourceUrlFormat = SourceUrlFormats.Gelbooru
    sourceUrlDomain = None
}

let Safebooru = KonachanSpider {
    name = "Safebooru"
    domain = "https://safebooru.org"
    requestFormat = RequestFormats.Gelbooru
    sourceUrlFormat = SourceUrlFormats.Gelbooru
    sourceUrlDomain = None
}

let HypnoHub = KonachanSpider {
    name = "HypnoHub"
    domain = "https://hypnohub.net"
    requestFormat = RequestFormats.HypnoHub
    sourceUrlFormat = SourceUrlFormats.Konachan
    sourceUrlDomain = None
}

let Spiders : ISpider list = [
    Konachan
    Lolibooru
    Gelbooru
    Yandere
    TheBigImageBoard
    Safebooru
    HypnoHub
]