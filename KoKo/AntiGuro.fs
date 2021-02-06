module KoKo.AntiGuro

let private guroTags = [
    // Lolibooru
    "guro"
    "asphyxiation"
    "autoerotic_asphyxiation"
    "archstanton"
    "crackaddict"
    "amputee"   // double_amputee quarter_amputee
    "death"
    "intestines"
    "wabaki"
    "eye_fuck"
    "eyes_rolled_back"
    "blood_on_face"
    "drill"
    "evil_grin"
    "evil_smile"
    "tongue_out"
    "decapitation"
    "head_fuck"
    "hair_grab"
    "blood_stain"
    "bloody_hair"
    "gua61"
    "dismemberment"
    "cut"   // cuts
    "grin"
    "bruise"
    "cum_inflation"
    "empty_eyes"
    "necrophilia"
]

let private guroTagsProcessed =
    guroTags
    |> List.map (fun x -> x.Trim().ToLower())

let isGuroTag (tag: string) =
    let tag = tag.Trim().ToLower();
    guroTags
    |> List.exists (fun x -> tag.IndexOf(x) <> -1)

let hasGuroTag tags = Seq.exists isGuroTag tags

let isGuroPost (post: KoKo.Post) = hasGuroTag post.tags

let antiGuro posts = Seq.filter (isGuroPost >> not) posts


