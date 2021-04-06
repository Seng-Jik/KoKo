module KoKo.SankakuComplex
open FSharp.Data

type private PostParser = JsonProvider<"SankakuComplexExample.json">

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
                |> Utils.takeWhileTimes 5 (function
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
