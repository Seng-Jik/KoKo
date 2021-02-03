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

            int index = 9;
            foreach(var spider in KoKo.AllSpiders.AllSpiders)
            {
                var checkBox = new CheckBox()
                {
                    Content = spider.Name,
                    IsChecked = true,
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

        private void Search_Click(object sender, RoutedEventArgs e)
        {
            tabViewItem.Header = SearchBox.Text;
            bool safe = Safe.IsChecked.GetValueOrDefault(false);
            bool questionable = Questionable.IsChecked.GetValueOrDefault(false);
            bool exp = Explicit.IsChecked.GetValueOrDefault(false);
            bool unknown = Unknown.IsChecked.GetValueOrDefault(false);

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
                        }
                    }
                }
            }

            var results = 
                from i in spiders
                select (
                    from post in i.Search(SearchBox.Text) 
                    where (
                        (post.rating == KoKo.Rating.Safe && safe) ||
                        (post.rating == KoKo.Rating.Questionable && questionable) ||
                        (post.rating == KoKo.Rating.Explicit && exp) ||
                        (post.rating == KoKo.Rating.Unknown && unknown))
                    select post);

            var search = new KoKo.Utils.MixEnumerable<KoKo.Post>(results.ToArray());
        }
    }
}
