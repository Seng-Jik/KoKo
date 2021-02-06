using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Popups;
using Microsoft.FSharp.Control;
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Linq;

namespace KoKoViewer
{
    public sealed class FavoritesData
    {
        static readonly FavoritesData fav = new FavoritesData();

        public static FavoritesData Get() => fav;


        ObservableCollection<Tuple<string, UInt64>> favortiePosts = new ObservableCollection<Tuple<string, ulong>>();
        HashSet<Tuple<string, UInt64>> favoritePostSet = new HashSet<Tuple<string, ulong>>();

        FavoritesData()
        {
            try
            {
                StorageFolder localFolder = ApplicationData.Current.RoamingFolder;
                StorageFile favorite = localFolder.GetFileAsync("favorites.dat").AsTask().Result;
                var a = FileIO.ReadBufferAsync(favorite).AsTask().Result;
                using (var s = DataReader.FromBuffer(a))
                {
                    var count = s.ReadInt32(); 
                    for(int i = 0; i < count; ++i)
                    {
                        var length = s.ReadUInt32();
                        var spider = s.ReadString(length).Trim();
                        var id = s.ReadUInt64();

                        favortiePosts.Add(Tuple.Create(spider, id));
                        favoritePostSet.Add(Tuple.Create(spider, id));
                    }
                }
                
            }
            catch (Exception) {
            }
        }

        public bool Has(string spiderName, UInt64 id) => favoritePostSet.Contains(Tuple.Create(spiderName, id));

        async void Flush()
        {
            try
            {
                StorageFolder localFolder = ApplicationData.Current.RoamingFolder;
                StorageFile favorite = await localFolder.CreateFileAsync("favorites.dat", CreationCollisionOption.ReplaceExisting);
                using (var a = await favorite.OpenAsync(FileAccessMode.ReadWrite))
                {
                    using (var s = new DataWriter(a))
                    {
                        s.WriteInt32(favortiePosts.Count);
                        foreach(var i in favortiePosts)
                        {
                            s.WriteUInt32(s.MeasureString(i.Item1));
                            s.WriteString(i.Item1);
                            s.WriteUInt64(i.Item2);
                        }
                        await s.StoreAsync();
                        await s.FlushAsync();
                        await a.FlushAsync();
                    }
                }
            }
            catch (Exception e) {
                await new MessageDialog(e.Message).ShowAsync();
            }
        }
        
        public void Add(string spiderName, UInt64 id)
        {
            favortiePosts.Insert(0, Tuple.Create(spiderName, id));
            favoritePostSet.Add(Tuple.Create(spiderName, id));
            Flush();
        }

        public void AddSome(IEnumerable<Tuple<string, UInt64>> some)
        {
            foreach(var i in some)
            {
                if(!Has(i.Item1, i.Item2))
                {
                    favortiePosts.Insert(0, i);
                    favoritePostSet.Add(i);
                }
            }

            Flush();
        }

        public void RemoveSome(IEnumerable<Tuple<string, UInt64>> some)
        {
            foreach (var i in some)
            {
                if (Has(i.Item1, i.Item2))
                {
                    favortiePosts.Remove(i);
                    favoritePostSet.Remove(i);
                }
            }

            Flush();
        }

        public void Remove(string spiderName, UInt64 id)
        {
            favortiePosts.Remove(Tuple.Create(spiderName, id));
            favoritePostSet.Remove(Tuple.Create(spiderName, id));
            Flush();
        }

        private static IEnumerable<KoKoViewerPost> Convert(Tuple<string, UInt64> post)
        {
            var s = 
                FSharpAsync.RunSynchronously(
                        (from spider in KoKo.AllSpiders.AllSpiders
                         where spider.Name == post.Item1
                         select spider).Single().GetPostById(post.Item2),
                        FSharpOption<int>.None,
                        FSharpOption<System.Threading.CancellationToken>.None);
            if (OptionModule.IsSome(s))
            {
                if(OptionModule.IsNone(s.Value.previewImage))
                    return SeqModule.Empty<KoKoViewerPost>();
                var p = new KoKoViewerPost() {
                    post = s.Value,
                    previewImageUrl = s.Value.previewImage.Value.imageUrl
                };
                return SeqModule.Singleton(p);
            }
            else return SeqModule.Empty<KoKoViewerPost>();
        }

        public BrowsePostSequence GetAllFavoritesSequence()
        {
            Func<IEnumerable<KoKoViewerPost>> creator = () =>
                SeqModule.Concat<IEnumerable<KoKoViewerPost>,KoKoViewerPost>(
                    from post in favortiePosts
                    select Convert(post));

            var seq = new BrowsePostSequence(creator);

            return seq;
        }
    }
}
