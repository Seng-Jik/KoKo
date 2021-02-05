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
        string imageUrl;
        KoKo.Post post;
        SearchOption searchOption;

        public Viewer()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var p = (Tuple<KoKo.Post, SearchOption>)e.Parameter;
            post = p.Item1;
            searchOption = p.Item2;

            imageUrl = post.images.First().Last().imageUrl;

            Flyout_ViewLarger.IsEnabled = post.images.First().Count() > 1;
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            var sc = ScrollViewer;
            sc.Width = ActualWidth;
            sc.Height = ActualHeight;
        }
        private void MainImage_ImageOpened(object sender, RoutedEventArgs e)
        {
            ProgressRing.IsActive = false;
            ProgressRing.Visibility = Visibility.Collapsed;
            var ratio1 = ActualHeight / MainImage.ActualHeight;
            var ratio2 = ActualWidth / MainImage.ActualWidth;
            var ratio = Math.Min(ratio1, ratio2);
            MainImage.Width = MainImage.ActualWidth * ratio;
            MainImage.Height = MainImage.ActualHeight * ratio;
        }

        private void MainImage_Tapped(object sender, TappedRoutedEventArgs e)
        {
            FlyoutBase.ShowAttachedFlyout((FrameworkElement)sender);
        }

        private void FlyoutTagList_ItemClick(object sender, ItemClickEventArgs e)
        {
            var newTab = new TabViewItem();
            var search = searchOption;
            search.SearchString = e.ClickedItem as string;

            // The Content of a TabViewItem is often a frame which hosts a page.
            Frame frame = new Frame();
            newTab.Content = frame;
            SearchPage.Search(search, ref newTab);

            MainPage.Get().InsertTabViewAfterCurrent(newTab);
        }

        private async void FlyoutSourceList_ItemClick(object sender, ItemClickEventArgs e)
        {
            var uri = new Uri(e.ClickedItem as string);
            await Windows.System.Launcher.LaunchUriAsync(uri);
        }

        private void Flyout_ViewLarger_Click(object sender, RoutedEventArgs e)
        {
            (sender as AppBarButton).IsEnabled = false;
            imageUrl = post.images.First().First().imageUrl;
            MainImage.Source = new BitmapImage(new Uri(imageUrl));

            Flyout.Hide();

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
            var bitmap = (MainImage.Source as BitmapImage);
            DisplayResolution.Text = $"{bitmap.PixelWidth}x{bitmap.PixelHeight}";

            if (post.score.IsValueSome)
            {
                DisplayScore.Text = $" Score:{post.score.Value}";
            }

            if (FavoritesData.Get().Has(post.fromSpider.Name, post.id))
                Flyout_Star.Icon = new SymbolIcon() { Symbol = Symbol.SolidStar };
            else
                Flyout_Star.Icon = new SymbolIcon() { Symbol = Symbol.OutlineStar };
        }
    }
}
