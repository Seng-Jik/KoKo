module KoKo.SankakuComplex
open FSharp.Data
open System.Net
type private PostParser = JsonProvider<"SankakuComplexExample.json">

let private accept = """text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9"""
let private userAgent = """Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/88.0.4324.150 Safari/537.36 Edg/88.0.705.63"""
let private downloadString (url: string) = async {
    use webClient = new WebClient ()
    webClient.Headers.Set(HttpRequestHeader.UserAgent, userAgent)
    webClient.Headers.Set(HttpRequestHeader.IfNoneMatch, "\"W/\"d77e-wCeuOPL54DbvEpMg2nCyp3TSW00")
    webClient.Headers.Set(HttpRequestHeader.CacheControl, "max-age=0")
    webClient.Headers.Set(HttpRequestHeader.AcceptLanguage, "zh-CN,zh;q=0.9,en;q=0.8,en-GB;q=0.7,en-US;q=0.6,zh-TW;q=0.5")
    webClient.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate, br")
    webClient.Headers.Set(HttpRequestHeader.Accept, accept)

    printfn "DS: %s" url
    try return (Ok <| webClient.DownloadString url)
    with e -> return (Error e)
}

type SankakuComplex () =
    interface ISpider with
        member x.Name = "Sankaku Channel"
        member x.All = Spider.search "" x
        member x.GetPostById id = async { return Spider.search $"id:{id}" x |> Seq.tryHead }
        member spider.Search tags =
            let pages =
                Seq.initInfinite (fun pageId ->
                    let mutable retry = 5
                    let mutable result : Result<PostParser.Root[], exn> = Error null
                    while retry > 0 do
                        try
                            sprintf "https://capi-v2.sankakucomplex.com/posts?page=%u&tags=%s" pageId tags
                            |> downloadString
                            |> Async.RunSynchronously
                            |> function
                            | Error e -> raise e
                            | Ok x -> result <- Ok (x |> PostParser.Parse)
                            retry <- 0
                        with e -> 
                            result <- Error e
                            retry <- retry - 1
                            System.Threading.Thread.Sleep 5000
                    match result with
                    | Ok x -> x
                    | Error e -> 
                        printfn "- Sankaku Spider"
                        printfn "Page Parsing Error:"
                        printfn "Page: %d" pageId
                        printfn "Spider: %s" <| Spider.name spider
                        printfn "%A" e
                        [||])
                |> Seq.takeWhile (fun x -> x.Length > 0)

            pages
            |> Seq.concat
            |> Seq.choose (fun x -> 
                try 
                    let mapToImage = 
                        Option.map (fun x -> {
                            imageUrl = x
                            fileName = Utils.getFileNameFromUrl x |> Utils.normalizeFileName
                        })

                    Some {
                        id = uint64 x.Id
                        fromSpider = spider
                        rating = Konachan.parseRating x.Rating
                        score = 
                            try x.TotalScore |> float |> ValueSome
                            with _ -> ValueNone

                        sourceUrl = [
                            $"https://chan.sankakucomplex.com/post/show/{x.Id}"
                            if x.Source.IsSome then
                                if System.String.IsNullOrWhiteSpace x.Source.Value |> not then
                                    x.Source.Value
                        ]

                        tags = x.Tags |> Seq.map (fun x -> x.NameEn)

                        previewImage = mapToImage x.PreviewUrl

                        images = [
                            [
                                let a = mapToImage x.FileUrl
                                let b = mapToImage x.SampleUrl

                                if a.IsSome then a.Value
                                if b.IsSome then b.Value
                            ]
                        ]
                    }
                with e -> 
                    printfn "- Danbooru Spider"
                    printfn "Post Parsing Error: %A" e
                    None)

let SankakuComplex : ISpider list = [
    SankakuComplex ()
]
