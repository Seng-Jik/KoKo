open KoKo
open System.IO
open FSharp.Data
open FSharp.Collections.ParallelSeq

System.IO.Directory.CreateDirectory "downloads" |> ignore

let LogFile = File.OpenWrite ("downloads/log.log")
let LogFileWriter = new StreamWriter (LogFile)

let Spiders : ISpider list =
    Konachan.Spiders
    //Konachan.Spiders @ Danbooru.Spiders


type FinishedListCsv = CsvProvider<"Konachan,0,\"a\"",HasHeaders = false, Schema="SpiderName (string),Id (int),FileName (string)">

let finishedListFile = "downloads/finished.csv"
let mutable FinishedList =
    if File.Exists finishedListFile then (FinishedListCsv.Parse <| File.ReadAllText finishedListFile).Cache()
    else (FinishedListCsv.Parse ",,").Cache()

printfn "Input Tags: "
let tags = System.Console.ReadLine().Trim()

let downloadDir = "downloads/" + tags + "/"
Directory.CreateDirectory downloadDir |> ignore

printfn "-- Spiders --"
let posts =
    Spiders
    |> PSeq.filter (fun x -> 
        if Spider.test x = Ok () then
            printfn "%s" x.Name
            true
        else false)
    |> PSeq.toArray
    |> Seq.map (Spider.search tags)

printfn "Press any key to continue..."
System.Console.ReadKey () |> ignore

let downloadingTask = System.Collections.Concurrent.ConcurrentDictionary<string, unit>()

let downloadPost (post: Post) = 
    if FinishedList.Rows |> Seq.exists (fun x -> x.SpiderName = post.fromSpider.Name && uint64 x.Id = post.id) |> not then
        post.images
        |> PSeq.iter (fun x -> 
            async {
                try
                    let c = sprintf "%s %d" post.fromSpider.Name post.id
                    downloadingTask.TryAdd (c, ()) |> ignore
                    match! Mipmaps.downloadBestImage x with
                    | Error e -> 
                        lock LogFile (fun () ->
                            LogFileWriter.WriteLine ("--------")
                            LogFileWriter.WriteLine (sprintf "%A" post)
                            LogFileWriter.WriteLine (sprintf "%A" e)
                            LogFile.Flush ())
                    | Ok (data, image) ->
                        let target = downloadDir + Utils.normalizeFileName image.fileName
                        File.WriteAllBytes (target, data)
                        let newRow = FinishedListCsv.Row (post.fromSpider.Name,int post.id,target)
                        lock finishedListFile (fun () ->
                            FinishedList <- FinishedList.Append [newRow]
                            let csv = FinishedList.SaveToString()
                            File.WriteAllText (finishedListFile, csv))
                    downloadingTask.TryRemove(c, ref ()) |> ignore
                with e ->
                    printfn "%A" e
            }
            |> Async.RunSynchronously)

let tasks = System.Collections.Concurrent.ConcurrentBag ()

async {
    posts
    |> Seq.toArray
    |> Utils.MixEnumerable
    |> KoKo.AntiGuro.antiGuro
    |> PSeq.iter (fun post -> async { downloadPost post } |> Async.StartAsTask |> tasks.Add)
}
|> Async.StartAsTask
|> tasks.Add

while true do
    System.Console.Clear ()
    printfn "= Downloading ="
    for i in downloadingTask do
        printfn "%s" i.Key
    System.Threading.Thread.Sleep(1000)

for i in tasks do
    i.Result

