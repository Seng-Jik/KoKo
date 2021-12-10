module KoKo.QwQAdapter

open FSharp.Control


let mapContent (c: QwQ.Content) =
    { fileName = c.FileName
      imageUrl = 
          let (QwQ.Https (url, _)) = c.DownloadMethod
          url
      headers = 
          let (QwQ.Https (_, c)) = c.DownloadMethod
          c.Headers}


let mapPost fromSpider (post: QwQ.Post) =
    { id = post.Id
      fromSpider = fromSpider 
      rating = 
          match post.Rating with
          | QwQ.Safe -> KoKo.Safe
          | QwQ.Questionable -> KoKo.Questionable
          | QwQ.Explicit -> KoKo.Explicit
          | QwQ.Unrated -> KoKo.Unknown
      score = ValueNone
      sourceUrl = AsyncSeq.toBlockingSeq post.SourceUrl
      tags = post.Tags
      previewImage = 
          post.PreviewImage 
          |> Option.map mapContent
      images = 
          post.Content 
          |> AsyncSeq.toBlockingSeq
          |> Seq.map AsyncSeq.toBlockingSeq
          |> Seq.map (Seq.map mapContent) }
    

let mapResult this (r: AsyncSeq<Result<QwQ.PostPage, exn>>) = 
    AsyncSeq.toBlockingSeq r
    |> Seq.collect (function
        | Ok x -> x
        | Error _ -> [])
    |> Seq.map (mapPost this)


type Adapter (src: QwQ.ISource) =
    interface KoKo.ISpider with
        member _.Name = src.Name
        member this.All = mapResult this src.AllPosts
        member this.Search tags =
            if System.String.IsNullOrWhiteSpace tags
            then mapResult this src.AllPosts
            else 
                match src with
                | :? QwQ.ISearch as s -> 
                    s.Search 
                        { QwQ.SearchOptions.ExludeTags = []
                          QwQ.SearchOptions.Order = QwQ.Default
                          QwQ.SearchOptions.Rating = QwQ.Unrated
                          QwQ.SearchOptions.Tags = 
                              if System.String.IsNullOrWhiteSpace tags
                              then [||]
                              else tags.Split ' ' }
                    |> mapResult this
                | _ -> Seq.empty
        member this.GetPostById id = 
            match src with
            | :? QwQ.IGetPostById as x ->
                x.GetPostById id
                |> QwQ.Utils.Async.map (QwQ.Utils.Result.toOption >> Option.flatten >> Option.map (mapPost this))
            | _ -> async { return None }


let sankakuComplexLoginInformation: (QwQ.Username * QwQ.Password) option =
    // 如果你需要登录SankakuComplex, 则可以修改以下登录信息
    None


let allSources = 
    QwQ.Sources.Sources.sources
    |> List.except 
        [ if sankakuComplexLoginInformation.IsSome then
              QwQ.Sources.SankakuComplex.sankakuChannel ]
    |> List.append 
        [ if sankakuComplexLoginInformation.IsSome then 
              let (u, p) = sankakuComplexLoginInformation.Value
              let login = QwQ.Sources.SankakuComplex.sankakuChannel :?> QwQ.ILogin<QwQ.Username, QwQ.Password>
              login.Login u p
              |> Async.RunSynchronously
              |> function
                  | Ok x -> x
                  | _ -> failwith "Login error!"]
    |> List.map (Adapter >> fun x -> x :> ISpider)

