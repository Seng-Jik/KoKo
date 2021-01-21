module KoKo.Konachan

open FSharp.Data

type RequestFormat = Printf.StringFormat<string->int->string->string>
type SourceUrlFormat = Printf.StringFormat<string->uint64->string>

type SpiderArguments = {
    name : string
    domain : string
    requestFormat : RequestFormat
    sourceUrlFormat : SourceUrlFormat
}

module RequestFormats =
    let Konachan : RequestFormat = "%s/post.xml?limit=100&page=%d&tags=%s"
    let HypnoHub : RequestFormat = "%s/post/index.xml?limit=100&page=%d&tags=%s"
    let Gelbooru : RequestFormat = "%s/index.php?page=dapi&s=post&q=index&&limit=100&pid=%d&tags=%s"

module SourceUrlFormats =
    let Konachan : SourceUrlFormat = "%s/post/show/%u"
    let Gelbooru : SourceUrlFormat = "%s/index.php?page=post&s=view&id=%u"


type PostXmlParser = XmlProvider<"https://konachan.net/post.xml?page=1&limit=100">

type KonachanSpider (args: SpiderArguments) =
    interface ISpider with
        member _.Name = args.name
        member x.All = (x :> ISpider).Search ""
        member spider.Search tags = 
            let pages =
                Seq.initInfinite (fun pageId ->
                    let mutable retry = 5
                    let mutable result : Result<PostXmlParser.Posts, exn> = Error null
                    while retry > 0 do
                        try
                            sprintf args.requestFormat args.domain pageId tags
                            |> PostXmlParser.Load
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
                rating = 
                    match x.Rating with
                    | "s" -> Safe
                    | "q" -> Questionable
                    | "e" -> Explicit
                    | _ -> Unknown
                score = x.Score |> float |> ValueSome
                sourceUrl = seq { 
                    sprintf args.sourceUrlFormat args.domain <| uint64 x.Id
                    if x.Source.IsSome then x.Source.Value 
                }
                tags = x.Tags.Split ' '
                previewImage = 
                    Some {
                        width = x.PreviewWidth
                        height = x.PreviewHeight
                        data = Utils.downloadData x.PreviewUrl
                        fileName = Utils.getFileNameFromUrl x.PreviewUrl
                    }

                images = seq {
                    seq {
                        {
                            width = x.Width
                            height = x.Height
                            data = Utils.downloadData x.FileUrl
                            fileName = Utils.getFileNameFromUrl x.FileUrl
                        }

                        let attrs = x.XElement.Attributes()
                        if attrs |> Seq.exists (fun x -> x.Name.LocalName = "jpeg_url") then
                            {
                                width = x.JpegWidth
                                height = x.JpegHeight
                                data = Utils.downloadData x.JpegUrl
                                fileName = Utils.getFileNameFromUrl x.JpegUrl
                            }

                        if attrs |> Seq.exists (fun x -> x.Name.LocalName = "sample_url") then
                            {
                                width = x.SampleWidth
                                height = x.SampleHeight
                                data = Utils.downloadData x.SampleUrl
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
}

let Lolibooru = KonachanSpider {
    name = "Lolibooru"
    domain = "https://lolibooru.moe"
    requestFormat = RequestFormats.Konachan
    sourceUrlFormat = SourceUrlFormats.Konachan
}

let Gelbooru = KonachanSpider {
    name = "Gelbooru"
    domain = "https://gelbooru.com"
    requestFormat = RequestFormats.Gelbooru
    sourceUrlFormat = SourceUrlFormats.Gelbooru
}

let Yandere = KonachanSpider {
    name = "Yandere"
    domain = "https://yande.re"
    requestFormat = RequestFormats.Konachan
    sourceUrlFormat = SourceUrlFormats.Konachan
}

let TheBigImageBoard = KonachanSpider {
    name = "The Big ImageBoard (TBIB)"
    domain = "https://tbib.org"
    requestFormat = RequestFormats.Gelbooru
    sourceUrlFormat = SourceUrlFormats.Gelbooru
}

let Safebooru = KonachanSpider {
    name = "Safebooru"
    domain = "https://safebooru.org"
    requestFormat = RequestFormats.Gelbooru
    sourceUrlFormat = SourceUrlFormats.Gelbooru
}

let HypnoHub = KonachanSpider {
    name = "HypnoHub"
    domain = "https://hypnohub.net"
    requestFormat = RequestFormats.HypnoHub
    sourceUrlFormat = SourceUrlFormats.Konachan
}

let AllGirl = KonachanSpider {
    name = "All Girl"
    domain = "https://allgirl.booru.org"
    requestFormat = RequestFormats.Gelbooru
    sourceUrlFormat = SourceUrlFormats.Gelbooru
}
