﻿module KoKo.Konachan

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


type private PostParser = XmlProvider<"KonachanExample.xml">

type KonachanSpider (args: SpiderArguments) =
    interface ISpider with
        member _.Name = args.name
        member x.All = (x :> ISpider).Search ""
        member x.GetPostById id = async { return Spider.search $"id:{id}" x |> Seq.tryHead }
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
                            | Ok x -> x |> XMLPreprocessor.PreprocessXML |> PostParser.Parse
                            |> fun xml -> result <- Ok xml
                            retry <- 0
                        with e -> 
                            result <- Error e
                            retry <- retry - 1
                            System.Threading.Thread.Sleep 5000
                    result)                     // 可以携带错误信息到最终结果里，以用来在UI上显示错误信息
            let head = Seq.head pages
            let pages = Seq.append [head] <| Seq.tail pages
            pages
            |> Utils.takeWhileTimes 3 (function
            | Ok x when x.Posts.Length > 0 -> true
            | _ -> false)
            |> Seq.choose (function
            | Ok x -> Some x.Posts
            | _ -> None)
            |> Seq.concat
            |> Seq.choose (fun x -> 
                try 
                    Some {
                        id = uint64 x.Id
                        fromSpider = spider
                        rating = parseRating x.Rating
                    
                        score = 
                            try x.Score |> float |> ValueSome
                            with _ -> ValueNone

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
                                headers = []
                            }

                        images = seq {
                            seq {
                                {
                                    imageUrl = x.FileUrl
                                    fileName = Utils.getFileNameFromUrl x.FileUrl
                                    headers = []
                                }

                                if x.JpegUrl.IsSome then
                                    {
                                        imageUrl = x.JpegUrl.Value
                                        fileName = Utils.getFileNameFromUrl x.JpegUrl.Value
                                        headers = []
                                    }

                                if x.SampleUrl.IsSome then
                                    {
                                        imageUrl = x.SampleUrl.Value
                                        fileName = Utils.getFileNameFromUrl x.SampleUrl.Value
                                        headers = []
                                    }
                            }
                        }
                    }
                with e -> 
                    printfn "- Konachan Spider"
                    printfn "Post Parsing Error: %A" e
                    printfn "Post: %u" x.Id
                    None)

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

let XBooru = KonachanSpider {
    name = "XBooru"
    domain = "https://xbooru.com"
    requestFormat = RequestFormats.Gelbooru
    sourceUrlFormat = SourceUrlFormats.Gelbooru
    sourceUrlDomain = None
}

let Rule34 = KonachanSpider {
    name = "Rule34"
    domain = "https://rule34.xxx"
    requestFormat = RequestFormats.Gelbooru
    sourceUrlFormat = SourceUrlFormats.Gelbooru
    sourceUrlDomain = None
}

let Realbooru = KonachanSpider {
    name = "Realbooru"
    domain = "https://realbooru.com"
    requestFormat = RequestFormats.Gelbooru
    sourceUrlFormat = SourceUrlFormats.Gelbooru
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
    XBooru
    Rule34
    Realbooru
]