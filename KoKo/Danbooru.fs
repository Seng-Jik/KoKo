module KoKo.Danbooru

open FSharp.Data

type private PostParser = XmlProvider<"DanbooruExample.xml">

type DanbooruSpider (name, domain) =
    interface ISpider with
        member _.Name = name
        member x.All = (x :> ISpider).Search ""
        member x.GetPostById id = async { return Spider.search $"id:{id}" x |> Seq.tryHead }
        member spider.Search tags =
            let pages = 
                Seq.initInfinite (fun pageId ->
                    let mutable retry = 5
                    let mutable result : Result<PostParser.Posts, exn> = Error null
                    while retry > 0 do
                        try
                            sprintf "%s/posts.xml?limit=50&page=%d&tags=%s" domain pageId tags
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
                    result)
                |> Utils.takeWhileTimes 3 (function
                | Ok x when x.Posts.Length > 0 -> true
                | _ -> false)
                |> Seq.choose (function
                | Ok x -> Some x.Posts
                | _ -> None)
                |> Seq.concat
            
            pages
            |> Seq.choose (fun x -> 
                try 
                    Some {
                        id = uint64 x.Id.Value
                        fromSpider = spider
                        rating = Konachan.parseRating x.Rating
                        score = 
                            try x.Score.Value |> float |> ValueSome
                            with _ -> ValueNone

                        sourceUrl = seq {
                            sprintf "%s/posts/%u" domain <| uint64 x.Id.Value
                            if x.Source.IsSome then
                                if not <| System.String.IsNullOrWhiteSpace x.Source.Value then
                                    x.Source.Value
                        }
                        tags = x.TagString.Trim().Split ' '
                        previewImage = 
                            x.PreviewFileUrl
                            |> Option.map (fun x -> {
                                imageUrl = x
                                fileName = Utils.getFileNameFromUrl x
                            })
                        images = seq {   
                            seq {
                                if x.LargeFileUrl.IsSome then
                                    if not <| System.String.IsNullOrWhiteSpace x.LargeFileUrl.Value then
                                        {
                                            imageUrl = x.LargeFileUrl.Value
                                            fileName = Utils.getFileNameFromUrl x.LargeFileUrl.Value
                                        }
                                {
                                    imageUrl = x.FileUrl
                                    fileName = Utils.getFileNameFromUrl x.FileUrl
                                }
                            }
                        }
                    } 
                with e -> 
                    printfn "- Danbooru Spider"
                    printfn "Post Parsing Error: %A" e
                    None)
       
let Danbooru = DanbooruSpider ("Danbooru", "https://danbooru.donmai.us")
let ATFbooru = DanbooruSpider ("ATFbooru", "https://booru.allthefallen.moe/")

let Spiders : ISpider list = [
    Danbooru
    ATFbooru
]