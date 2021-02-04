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

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace KoKoViewer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Viewer : Page
    {
        string imageUrl;

        public Viewer()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var post = e.Parameter as KoKo.Post;

            imageUrl = post.images.First().Last().imageUrl;
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
    }
}
