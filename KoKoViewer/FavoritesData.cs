using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.UI.Popups;

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

        public void Remove(string spiderName, UInt64 id)
        {
            favortiePosts.Remove(Tuple.Create(spiderName, id));
            favoritePostSet.Remove(Tuple.Create(spiderName, id));
            Flush();
        }

        //BrowsePostSequence GetAllFavorites();
    }
}
