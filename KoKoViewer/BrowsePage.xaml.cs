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
using Windows.UI.Popups;

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
        BrowsePostSequence posts;
        SearchOption searchOption;

        public BrowsePage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            
            var p = (Tuple<BrowsePostSequence, SearchOption, TabViewItem>)e.Parameter;
            posts = p.Item1;
            searchOption = p.Item2;
            posts.CollectionChanged += (o, ee) =>
            {
                ProgressRing.Visibility = Visibility.Collapsed;
                ProgressRingLoadingMore.Visibility = Visibility.Visible;
                p.Item3.IconSource = new SymbolIconSource { Symbol = Symbol.BrowsePhotos };
            };

            posts.HasNoMoreNow += async () => 
                await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () => 
                {
                    ProgressRing.Visibility = Visibility.Collapsed;
                    ProgressRingLoadingMore.Visibility = Visibility.Collapsed;
                    LoadingMoreHint.Height = new GridLength(0);
                    p.Item3.IconSource = new SymbolIconSource { Symbol = Symbol.BrowsePhotos };
                    
                });
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
                if (value != selectionModeInner)
                {
                    MutiSelectionCommandBar_OpenThese.IsEnabled = false;
                    MutiSelectionCommandBar_FavoriteThese.IsEnabled = false;
                    MutiSelectionCommandBar_UnFavoriteThese.IsEnabled = false;
                    MutiSelectionCommandBar_Download.IsEnabled = false;
                    MutiSelectionCommandBar_FavDownload.IsEnabled = false;


                    selectionModeInner = value;
                    MutiSelectionCommandBar.IsOpen = value;
                    if (value)
                    {
                        Browser.SelectionMode = ListViewSelectionMode.Multiple;
                    }
                    else
                    {
                        Browser.SelectedItems.Clear();
                        Browser.SelectionMode = ListViewSelectionMode.None;
                    }
                }
            }
        }
        private void Browser_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            selectionMode = !selectionMode;
        }

        private void Browser_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            bool enabled = Browser.SelectedItems.Count > 0;

            MutiSelectionCommandBar_OpenThese.IsEnabled = enabled;
            MutiSelectionCommandBar_FavoriteThese.IsEnabled = enabled;
            MutiSelectionCommandBar_UnFavoriteThese.IsEnabled = enabled;
            MutiSelectionCommandBar_Download.IsEnabled = enabled;
            MutiSelectionCommandBar_FavDownload.IsEnabled = enabled;
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

            var parent = (image.Parent as Grid);
            var stackPanel = parent.Children
                .Where(item => null != item as StackPanel)
                .First() as StackPanel;
                

            if (FavoritesData.Get().Has(post.fromSpider.Name, post.id))
                (stackPanel.FindName("Info_Fav") as SymbolIcon).Visibility = Visibility.Visible;

            if (DownloadHelper.GetDownloaded(post) != null)
                (stackPanel.FindName("Info_Downloaded") as SymbolIcon).Visibility = Visibility.Visible;

            var imageName = post.images.First().First().fileName.ToLower().Trim();

            if (imageName.EndsWith(".gif"))
                (stackPanel.FindName("Info_GIF") as SymbolIcon).Visibility = Visibility.Visible;

            else if (imageName.EndsWith(".mp4") || imageName.EndsWith(".webm"))
                (stackPanel.FindName("Info_Video") as SymbolIcon).Visibility = Visibility.Visible; ;
        }
    }
}
