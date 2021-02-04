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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace KoKoViewer
{
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
            tabViewItem = e.Parameter as TabViewItem;
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
            tabViewItem.Header = SearchBox.Text;
            if (String.IsNullOrWhiteSpace(tabViewItem.Header as string))
                tabViewItem.Header = "Browser";

            tabViewItem.IconSource = new SymbolIconSource() { Symbol = Symbol.BrowsePhotos };

            var spiders = new List<KoKo.ISpider>();
            foreach(var checkbox in MainStackPanel.Children)
            {
                if(checkbox is CheckBox)
                {
                    var c = checkbox as CheckBox;
                    if(c.Tag != null && c.IsChecked.GetValueOrDefault(false))
                    {
                        if(c.Tag is KoKo.ISpider)
                        {
                            spiders.Add(c.Tag as KoKo.ISpider);
                            settings.Spiders.Add((c.Tag as KoKo.ISpider).Name.Trim());
                        }
                    }
                }
            }

            settings.Save();

            string tags = SearchBox.Text;

            Func<IEnumerable<KoKoViewerPost>> f = () =>
            {
                var results =
                    from i in spiders
                    select (
                        from post in i.Search(tags)
                        where (
                            (post.rating == KoKo.Rating.Safe && safe) ||
                            (post.rating == KoKo.Rating.Questionable && questionable) ||
                            (post.rating == KoKo.Rating.Explicit && exp) ||
                            (post.rating == KoKo.Rating.Unknown && unknown))
                        where (Microsoft.FSharp.Core.OptionModule.IsSome(post.previewImage))
                        select post);
                var posts = new KoKo.Utils.MixEnumerable<KoKo.Post>(results.ToArray());
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

            Frame.Navigate(typeof(BrowsePage), new BrowsePostSequence(f));
        }
    }
}
