module KoKo.SankakuComplex
open FSharp.Data

type private PostParser = JsonProvider<"SankakuComplexExample.json">

let private fixUrlPrefix (x: string) =
    if x.StartsWith "//" then "https:" + x
    else x

type SankakuComplex (name, urlBase, sourceBase) =
    interface ISpider with
        member x.Name = name
        member x.All = Spider.search "" x
        member x.GetPostById id = async { return Spider.search $"id:{id}" x |> Seq.tryHead }
        member spider.Search tags =
            let pages =
                Seq.initInfinite (fun pageId ->
                    let mutable retry = 5
                    let mutable result : Result<PostParser.Root[], exn> = Error null
                    while retry > 0 do
                        try
                            sprintf "%s?page=%u&tags=%s" urlBase pageId tags
                            |> Utils.downloadString
                            |> Async.RunSynchronously
                            |> function
                            | Error e -> raise e
                            | Ok x -> result <- Ok (x |> PostParser.Parse)
                            retry <- 0
                        with e -> 
                            result <- Error e
                            retry <- retry - 1
                            System.Threading.Thread.Sleep 5000
                    result)
                |> Utils.takeWhileTimes 3 (function
                | Ok x when x.Length > 0 -> true
                | _ -> false)
                |> Seq.choose (function
                | Ok x -> Some x
                | _ -> None)

            pages
            |> Seq.concat
            |> Seq.choose (fun x -> 
                try 
                    let mapToImage = 
                        Option.map (fun x -> {
                            imageUrl = fixUrlPrefix <| x
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
                            $"{sourceBase}{x.Id}"
                            if x.Source.IsSome then
                                if System.String.IsNullOrWhiteSpace x.Source.Value |> not then
                                    x.Source.Value
                        ]

                        tags = 
                            x.Tags 
                            |> Seq.choose (fun x -> 
                                x.JsonValue.TryGetProperty("name")
                                |> Option.orElse (x.JsonValue.TryGetProperty("name_en"))
                                |> Option.orElse (x.JsonValue.TryGetProperty("name_jp")))
                            |> Seq.map JsonExtensions.AsString

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
    SankakuComplex (
        "Sankaku Channel", 
        "https://capi-v2.sankakucomplex.com/posts", 
        "https://chan.sankakucomplex.com/post/show/")
    SankakuComplex (
        "Sankaku Idol", 
        "https://iapi.sankakucomplex.com/post/index.json",
        "https://idol.sankakucomplex.com/post/show/")
]
