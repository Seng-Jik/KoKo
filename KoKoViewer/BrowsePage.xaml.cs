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
using Microsoft.FSharp.Core;
using Microsoft.UI.Xaml.Controls;
using SymbolIconSource = Microsoft.UI.Xaml.Controls.SymbolIconSource;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace KoKoViewer
{
    public struct KoKoViewerPost
    {
        public KoKo.Post post;
        public string previewImageUrl;
    }

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class BrowsePage : Page
    {
        ObservableCollection<KoKoViewerPost> posts;
        SearchOption searchOption;

        public BrowsePage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            
            var p = (Tuple<ObservableCollection<KoKoViewerPost>, SearchOption>)e.Parameter;
            posts = p.Item1;
            searchOption = p.Item2;
            posts.CollectionChanged += (o, ee) =>
            {
                ProgressRing.Visibility = Visibility.Collapsed;
            };
        }

        private void Browser_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (!selectionMode)
            {
                var post = ((KoKoViewerPost)e.ClickedItem).post;

                var newTab = new TabViewItem();
                newTab.IconSource = new SymbolIconSource() { Symbol = Symbol.Pictures };
                newTab.Header = post.fromSpider.Name + " " + post.id;

                // The Content of a TabViewItem is often a frame which hosts a page.
                Frame frame = new Frame();
                newTab.Content = frame;
                frame.Navigate(typeof(Viewer), Tuple.Create(post, searchOption, newTab));


                MainPage.Get().InsertTabViewAfterCurrent(newTab);
            }
        }

        bool selectionModeInner = false;
        bool selectionMode
        {
            get => selectionModeInner;
            set {
                selectionModeInner = value;
                MutiSelectionCommandBar.IsOpen = value;
                if (value)
                {
                    Browser.SelectionMode = ListViewSelectionMode.Multiple;
                    MutiSelectionCommandBar.IsEnabled = false;
                }
                else
                {
                    Browser.SelectedItems.Clear();
                    Browser.SelectionMode = ListViewSelectionMode.None;
                    MutiSelectionCommandBar.IsEnabled = false;
                }
            }
        }
        private void Browser_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            selectionMode = !selectionMode;
        }

        private void Browser_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MutiSelectionCommandBar.IsEnabled = Browser.SelectedItems.Count > 0;
        }

        private void MutiSelectionCommandBar_OpenThese_Click(object sender, RoutedEventArgs e)
        {
            foreach (var i in Browser.SelectedItems)
            {
                var post = ((KoKoViewerPost)i).post;

                var newTab = new TabViewItem();
                newTab.IconSource = new SymbolIconSource() { Symbol = Symbol.Pictures };
                newTab.Header = post.fromSpider.Name + " " + post.id;

                // The Content of a TabViewItem is often a frame which hosts a page.
                Frame frame = new Frame();
                newTab.Content = frame;
                frame.Navigate(typeof(Viewer), Tuple.Create(post, searchOption, newTab));


                MainPage.Get().InsertTabViewAfterCurrent(newTab, false);
            }
            selectionMode = false;
        }

        private void MutiSelectionCommandBar_FavoriteThese_Click(object sender, RoutedEventArgs e)
        {
            FavoritesData.Get().AddSome(
                from post in Browser.SelectedItems
                select (Tuple.Create(
                    ((KoKoViewerPost)post).post.fromSpider.Name,
                    ((KoKoViewerPost)post).post.id)));
            selectionMode = false;
        }

        private void MutiSelectionCommandBar_UnFavoriteThese_Click(object sender, RoutedEventArgs e)
        {
            FavoritesData.Get().RemoveSome(
                from post in Browser.SelectedItems
                select (Tuple.Create(
                    ((KoKoViewerPost)post).post.fromSpider.Name,
                    ((KoKoViewerPost)post).post.id)));
            selectionMode = false;
        }

        private void MutiSelectionCommandBar_Cancel_Click(object sender, RoutedEventArgs e)
        {
            selectionMode = false;
        }

        private void MutiSelectionCommandBar_Download_Click(object sender, RoutedEventArgs e)
        {
            foreach (var i in Browser.SelectedItems)
            {
                var post = ((KoKoViewerPost)i).post;
                if (DownloadHelper.GetDownloaded(post) == null)
                    DownloadHelper.Download(post);
            }
            selectionMode = false;
        }

        private void MutiSelectionCommandBar_FavDownload_Click(object sender, RoutedEventArgs e)
        {
            FavoritesData.Get().AddSome(
                from post in Browser.SelectedItems
                select (Tuple.Create(
                    ((KoKoViewerPost)post).post.fromSpider.Name,
                    ((KoKoViewerPost)post).post.id)));
            foreach (var i in Browser.SelectedItems)
            {
                var post = ((KoKoViewerPost)i).post;
                if (DownloadHelper.GetDownloaded(post) == null)
                    DownloadHelper.Download(post);
            }
            selectionMode = false;
        }

        private void Image_ImageOpened(object sender, RoutedEventArgs e)
        {
            var image = sender as Image;
            var post = image.Tag as KoKo.Post;

            var parent = image.Parent as Grid;

            if (FavoritesData.Get().Has(post.fromSpider.Name, post.id))
                parent.FindName("Info_Fav");

            if (DownloadHelper.GetDownloaded(post) != null)
                parent.FindName("Info_Downloaded");

            var imageName = post.images.First().First().fileName.ToLower().Trim();

            if (imageName.EndsWith(".gif"))
                image.FindName("Info_GIF");

            if (imageName.EndsWith(".mp4") || imageName.EndsWith(".webm"))
                image.FindName("Info_Video");
        }
    }
}
