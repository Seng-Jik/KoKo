using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace KoKoViewer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Viewer : Page
    {
        KoKo.Post post;
        SearchOption searchOption;

        public Viewer()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var p = (Tuple<KoKo.Post, SearchOption, TabViewItem>)e.Parameter;
            post = p.Item1;
            searchOption = p.Item2;
            var tabViewItem = p.Item3;

            var cache = DownloadHelper.GetDownloaded(post);

            var fileName = post.images.First().First().fileName.ToLower();

            if (fileName.EndsWith(".mp4") || fileName.EndsWith(".webm"))
            {
                // 如果是视频
                Flyout_ViewLarger.IsEnabled = false;
                FindName("MediaPlayer");
                MediaPlayer.MediaPlayer.IsLoopingEnabled = true;
                MediaPlayer.MediaPlayer.AutoPlay = false;
                MediaPlayer.TransportControls.ShowAndHideAutomatically = false;
                MediaPlayer.TransportControls.IsCompact = true;
                MediaPlayer.TransportControls.IsSkipBackwardButtonVisible = true;
                MediaPlayer.TransportControls.IsSkipForwardButtonVisible = true;
                MediaPlayer.TransportControls.IsSkipForwardEnabled = true;
                MediaPlayer.TransportControls.IsSkipBackwardEnabled = true;
                MediaPlayer.TransportControls.IsZoomButtonVisible = true;
                MediaPlayer.TransportControls.IsZoomEnabled = true;
                tabViewItem.CloseRequested += (o, ee) =>
                {
                    MediaPlayer.MediaPlayer.Pause();
                    MediaPlayer.MediaPlayer.Dispose();
                };

                if (cache != null)
                {
                    MediaPlayer.Source = Windows.Media.Core.MediaSource.CreateFromStorageFile(cache);
                }
                else
                {
                    MediaPlayer.Source = Windows.Media.Core.MediaSource.CreateFromUri(new Uri(post.images.First().First().imageUrl));
                }
            }
            else
            {
                // 如果是图片
                FindName("ScrollViewer");
                ProgressRing.IsActive = true;
                if (cache != null)
                {
                    Flyout_ViewLarger.IsEnabled = false;
                    ImageSource.SetSource(cache.OpenAsync(FileAccessMode.Read).AsTask().Result);
                }
                else
                {
                    if (post.images.First().First().fileName.ToLower().EndsWith(".gif"))
                    {
                        ImageSource.UriSource = new Uri(post.images.First().First().imageUrl);
                        Flyout_ViewLarger.IsEnabled = false;
                    }
                    else
                    {
                        ImageSource.UriSource = new Uri(post.images.First().Last().imageUrl);
                        Flyout_ViewLarger.IsEnabled = post.images.First().Count() > 1;
                    }
                }
            }
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var sc = ScrollViewer;
            if (sc != null && sc.IsLoaded)
            {
                sc.Width = ActualWidth;
                sc.Height = ActualHeight;
            }

            if(MediaPlayer != null && MediaPlayer.IsLoaded)
            {
                MediaPlayer.Width = ActualWidth;
                MediaPlayer.Height = ActualHeight;
            }
        }

        private void MainImage_Tapped(object sender, TappedRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout(MainGrid);
        }

        private void FlyoutTagList_ItemClick(object sender, ItemClickEventArgs e)
        {
            MainPage.Get().NewPage(e.ClickedItem as string + " ");
        }

        private async void FlyoutSourceList_ItemClick(object sender, ItemClickEventArgs e)
        {
            var uri = new Uri(e.ClickedItem as string);
            await Windows.System.Launcher.LaunchUriAsync(uri);
        }

        private void Flyout_ViewLarger_Click(object sender, RoutedEventArgs e)
        {
            (sender as AppBarButton).IsEnabled = false;
            var imageUrl = post.images.First().First().imageUrl;
            ImageSource.UriSource = new Uri(imageUrl);

            Flyout.Hide();

            ProgressRing.Value = 0;
            ProgressRing.IsIndeterminate = true;
            ProgressRing.IsActive = true;
            ProgressRing.Visibility = Visibility.Visible;
        }

        private void Flyout_Star_Click(object sender, RoutedEventArgs e)
        {
            if (FavoritesData.Get().Has(post.fromSpider.Name, post.id))
                FavoritesData.Get().Remove(post.fromSpider.Name, post.id);
            else FavoritesData.Get().Add(post.fromSpider.Name, post.id);

            Flyout_Opening(null, null);
        }

        private void Flyout_Opening(object sender, object e)
        {
            if (MainImage != null && MainImage.IsLoaded)
            {
                DisplayResolution.Text = $"{ImageSource.PixelWidth}x{ImageSource.PixelHeight}";
            }
            else if (MediaPlayer != null && MediaPlayer.IsLoaded)
            {
                //DisplayResolution.Text = $"{MediaPlayer.}x{MediaPlayer.NaturalVideoHeight}";
            }
            else DisplayResolution.Text = "";

            if (post.score.IsValueSome)
            {
                DisplayScore.Text = $" Score:{post.score.Value}";
            }

            if(DownloadHelper.GetDownloaded(post) != null)
            {
                Flyout_Download.IsEnabled = false;
                Flyout_ViewLarger.IsEnabled = false;
                Flyout_Download.Label = "OK!";
            }

            if (FavoritesData.Get().Has(post.fromSpider.Name, post.id))
            {
                Flyout_Star.Icon = new SymbolIcon() { Symbol = Symbol.UnFavorite };
                Flyout_Star.Label = "UnFavorite";
            }
            else
            {
                Flyout_Star.Icon = new SymbolIcon() { Symbol = Symbol.Favorite };
                Flyout_Star.Label = "Favorite";
            }
        }

        private void BitmapImage_ImageOpened(object sender, RoutedEventArgs e)
        {
            ProgressRing.IsActive = false;
            ProgressRing.Visibility = Visibility.Collapsed;
            var ratio1 = ActualHeight / MainImage.ActualHeight;
            var ratio2 = ActualWidth / MainImage.ActualWidth;
            var ratio = Math.Min(ratio1, ratio2);
            MainImage.Width = MainImage.ActualWidth * ratio;
            MainImage.Height = MainImage.ActualHeight * ratio;
        }

        private void ImageSource_ImageFailed(object sender, ExceptionRoutedEventArgs e)
        {
            ProgressRing.IsActive = false;
            ProgressRing.Visibility = Visibility.Collapsed;
        }

        private void ImageSource_DownloadProgress(object sender, DownloadProgressEventArgs e)
        {
            ProgressRing.IsIndeterminate = false;
            ProgressRing.Value = e.Progress;
        }

        private void Flyout_Download_Click(object sender, RoutedEventArgs e)
        {
            Flyout_Download.IsEnabled = false;
            DownloadHelper.Download(post);
        }

        bool mediaPlayerTransportControlShown = true;
        private void MediaPlayer_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (!mediaPlayerTransportControlShown)
                MediaPlayer.TransportControls.Show();
            else MediaPlayer.TransportControls.Hide();

            mediaPlayerTransportControlShown = !mediaPlayerTransportControlShown;
        }

        private void MediaPlayer_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout(MainGrid);
        }

        private void MediaPlayer_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout(MainGrid);
        }

        private void MainImage_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout(MainGrid);
        }
    }
}
