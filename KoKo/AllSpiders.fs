module KoKo.AllSpiders

let AllSpiders = 
    [
        Konachan.Spiders
        SankakuComplex.SankakuComplex
        Danbooru.Spiders
    ]
    |> Seq.concat

