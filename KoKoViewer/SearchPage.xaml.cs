using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;
using SymbolIconSource = Microsoft.UI.Xaml.Controls.SymbolIconSource;
using System.Collections.ObjectModel;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace KoKoViewer
{

    public struct SearchOption
    {
        public bool Safe, Questionable, Explicit, Unknown;
        public string SearchString;
        public List<KoKo.ISpider> Spiders;
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SearchPage : Page
    {
        TabViewItem tabViewItem;

        public SearchPage()
        {
            this.InitializeComponent();

            var settings = SearchSettings.Load();
            Safe.IsChecked = settings.RatingSafe;
            Questionable.IsChecked = settings.RatingQuestionable;
            Explicit.IsChecked = settings.RatingExplicit;
            Unknown.IsChecked = settings.RatingUnknown;

            int index = 10;
            foreach(var spider in KoKo.AllSpiders.AllSpiders)
            {
                var checkBox = new CheckBox()
                {
                    Content = spider.Name,
                    IsChecked = settings.Spiders.Contains(spider.Name.Trim()),
                    Tag = spider
                };

                MainStackPanel.Children.Insert(index++, checkBox);
            }
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var param = e.Parameter as Tuple<TabViewItem, string>;
            tabViewItem = param.Item1;
            SearchBox.Text = param.Item2;
        }

        public static void Search(SearchOption searchOption, ref TabViewItem searchTab)
        {
            searchTab.Header = searchOption.SearchString;
            if (String.IsNullOrWhiteSpace(searchTab.Header as string))
                searchTab.Header = "Browser";

            searchTab.IconSource = new SymbolIconSource() { Symbol = Symbol.BrowsePhotos };

            Func<IEnumerable<KoKoViewerPost>> f = () =>
            {
                var results =
                    from i in searchOption.Spiders
                    select (
                        from post in i.Search(searchOption.SearchString)
                        where (
                            (post.rating == KoKo.Rating.Safe && searchOption.Safe) ||
                            (post.rating == KoKo.Rating.Questionable && searchOption.Questionable) ||
                            (post.rating == KoKo.Rating.Explicit && searchOption.Explicit) ||
                            (post.rating == KoKo.Rating.Unknown && searchOption.Unknown))
                        where (OptionModule.IsSome(post.previewImage))
                        select post);
                var posts = new KoKo.Utils.MixEnumerable<KoKo.Post>(results.ToArray()) as IEnumerable<KoKo.Post>;
                posts = KoKo.AntiGuro.antiGuro(posts);      // Anti Guro
                var postEnum =
                    from post in posts
                    select (
                        new KoKoViewerPost()
                        {
                            post = post,
                            previewImageUrl =
                                OptionModule.DefaultValue("",
                                    OptionModule.Map(
                                        FSharpFunc<KoKo.Image, string>.FromConverter(x => x.imageUrl),
                                        post.previewImage))
                        });
                return postEnum;
            };

            (searchTab.Content as Frame).Navigate(typeof(BrowsePage), Tuple.Create(new BrowsePostSequence(f) as ObservableCollection<KoKoViewerPost>, searchOption));
        }

        private void Search_Click(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs e)
        {
            bool safe = Safe.IsChecked.GetValueOrDefault(false);
            bool questionable = Questionable.IsChecked.GetValueOrDefault(false);
            bool exp = Explicit.IsChecked.GetValueOrDefault(false);
            bool unknown = Unknown.IsChecked.GetValueOrDefault(false);

            var settings = new SearchSettings();
            settings.RatingSafe = safe;
            settings.RatingQuestionable = questionable;
            settings.RatingExplicit = exp;
            settings.RatingUnknown = unknown;

            // Search
            var searchOption = new SearchOption()
            {
                Safe = safe,
                Questionable = questionable,
                Explicit = exp,
                Unknown = unknown,
                SearchString = SearchBox.Text,
                Spiders = new List<KoKo.ISpider>()
            };
            
            foreach(var checkbox in MainStackPanel.Children)
            {
                if(checkbox is CheckBox)
                {
                    var c = checkbox as CheckBox;
                    if(c.Tag != null && c.IsChecked.GetValueOrDefault(false))
                    {
                        if(c.Tag is KoKo.ISpider)
                        {
                            searchOption.Spiders.Add(c.Tag as KoKo.ISpider);
                            settings.Spiders.Add((c.Tag as KoKo.ISpider).Name.Trim());
                        }
                    }
                }
            }

            settings.Save();

            Search(searchOption, ref tabViewItem);
        }
    }
}
