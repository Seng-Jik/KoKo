module KoKo.AllSpiders

let AllSpiders = 
    [
        Konachan.Spiders
        Danbooru.Spiders
    ]
    |> Seq.concat

