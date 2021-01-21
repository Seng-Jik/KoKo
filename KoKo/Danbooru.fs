module KoKo.Danbooru

open FSharp.Data

type PostParser = XmlProvider<"https://danbooru.donmai.us/posts.xml?limit=100">

type DanbooruSpider (name, domain) =
    interface ISpider with
        member _.Name = name
        member x.All = (x :> ISpider).Search ""
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
                            | Ok x -> x |> Utils.normalizeXml |> PostParser.Parse
                            |> fun xml -> result <- Ok xml
                            retry <- 0
                        with e -> 
                            result <- Error e
                            retry <- retry - 1
                    match result with
                    | Ok x -> x.Posts
                    | Error e -> [||])   // TODO: Report x
                |> Seq.takeWhile (fun x -> x.Length > 0)    // Bug Here.
                |> Seq.concat
            
            pages
            |> Seq.choose (fun x -> 
                try 
                    Some {
                        id = uint64 x.Id.Value
                        fromSpider = spider
                        rating = Konachan.parseRating x.Rating
                        score = x.Score.Value |> float |> ValueSome

                        sourceUrl = seq {
                            sprintf "%s/posts/%u" domain <| uint64 x.Id.Value
                            if x.Source.IsSome then
                                if not <| System.String.IsNullOrWhiteSpace x.Source.Value then
                                    x.Source.Value
                        }
                        tags = x.TagString.Trim().Split ' '
                        previewImage = 
                            Some {
                                imageUrl = x.PreviewFileUrl
                                fileName = Utils.getFileNameFromUrl x.PreviewFileUrl
                            }
                        images = seq {   
                            seq {
                                let attrs = x.XElement.Elements()
                                if attrs |> Seq.exists (fun x -> x.Name.LocalName = "large-file-url") then
                                    {
                                        imageUrl = x.LargeFileUrl
                                        fileName = Utils.getFileNameFromUrl x.LargeFileUrl
                                    }
                                {
                                    imageUrl = x.FileUrl
                                    fileName = Utils.getFileNameFromUrl x.FileUrl
                                }
                            }
                        }
                    } 
                with _ -> None)
       
let Danbooru = DanbooruSpider ("Danbooru", "https://danbooru.donmai.us")
let ATFBooru = DanbooruSpider ("ATFBooru", "https://booru.allthefallen.moe/")

let Spiders : ISpider list = [
    Danbooru
    ATFBooru
]