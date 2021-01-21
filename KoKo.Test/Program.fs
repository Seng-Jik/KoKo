open KoKo

open System.IO

let Spiders = 
    Danbooru.Spiders
    @ Konachan.Spiders

if Directory.Exists "downloads" then
    Directory.Delete ("downloads", true)
Directory.CreateDirectory "downloads" |> ignore


Spiders
|> List.toArray
|> Array.Parallel.map (fun spider ->
    let name = Spider.name spider

    Directory.CreateDirectory ("downloads/" + name) |> ignore

    let p =
        Spider.all spider
        |> Seq.head

    lock stdout (fun () ->
        printfn "=== %s ===" name
        printfn "ID: %u" p.id
        printfn "Rating: %A" p.rating
        printfn "Score: %A" p.score
        printfn "Source:"
        for i in p.sourceUrl do printfn "\t%s" i

        printf "Tags: "
        for i in (if Seq.length p.tags > 3 then Seq.take 3 p.tags else p.tags) do printf "%s, " i
        printfn "..."

        printfn ""
        printfn ""
        printfn ""
        
        p))
|> Array.iter (fun post ->
    printf "-- %s ..." post.fromSpider.Name
    let target = "downloads/" + Spider.name post.fromSpider + "/"
    [| 
        async {
            match post.previewImage with
            | None -> ()
            | Some pv ->
                match! Image.download pv with
                | Error e -> raise e
                | Ok data -> 
                    Directory.CreateDirectory (target + "Preview") |> ignore
                    File.WriteAllBytes (target + "Preview/" + pv.fileName, data)
        }


        yield! (
            post.images
            |> Seq.toArray
            |> Array.mapi (fun index i ->
                let targetDir = "downloads/" + post.fromSpider.Name + "/Image " + string index
                Directory.CreateDirectory targetDir |> ignore
                targetDir, i)
            |> Array.collect (fun (targetDir, mipmaps) -> 
                mipmaps
                |> Seq.toArray
                |> Array.mapi (fun i x ->
                    let targetDir = targetDir + "/Mipmap " + string i + "/"
                    async {
                        match! Image.download x with
                        | Error e -> raise e
                        | Ok data -> 
                            Directory.CreateDirectory targetDir |> ignore
                            File.WriteAllBytes (targetDir + x.fileName, data)
                    }
                )
            )
        )
    |]
    |> Async.Parallel
    |> Async.RunSynchronously
    |> ignore
    printfn "OK")