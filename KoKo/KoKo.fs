namespace KoKo

type Image = {
    width : int
    height : int
    download : byte[] Async
    fileName : string
    extFileName : string
}

type Rating =
| Safe
| Questionable
| Explicit
| Unknown

type Post = {
    id : uint64
    fromSpider : ISpider

    rating : Rating
    score : float voption
    sourceUrl : string seq
    tags : string seq
    
    origin : Image
    previewImage : Image option
    jpeg : Image option
    sample : Image option
}

and ISpider =
    abstract Name : string
    abstract All : Post seq
    abstract Search : string list -> Post seq
