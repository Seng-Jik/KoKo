open KoKo


System.IO.Directory.CreateDirectory "downloads" |> ignore

Spider.all Konachan.AllGirl
|> Seq.take 10
|> Seq.collect (fun x -> x.images)
|> Seq.map (fun x -> async {
    let! data = Mipmaps.downloadBestImage x
    match data with
    | Error e -> printfn "%A" e
    | Ok (data, info) ->
        printfn "%A" info
        System.IO.File.WriteAllBytes ("downloads/" + info.fileName, data)
    })
|> Seq.toArray
|> Async.Parallel
|> Async.Ignore
|> Async.RunSynchronously
