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

        public BrowsePage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            
            posts = e.Parameter as BrowsePostSequence;
            posts.CollectionChanged += (o, ee) =>
            {
                ProgressRing.Visibility = Visibility.Collapsed;
            };
        }

        private void Image_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var post = (sender as Image).Tag as KoKo.Post;

            var newTab = new TabViewItem();
            newTab.IconSource = new SymbolIconSource() { Symbol = Symbol.Pictures };
            newTab.Header = post.fromSpider.Name + " " + post.id;

            // The Content of a TabViewItem is often a frame which hosts a page.
            Frame frame = new Frame();
            newTab.Content = frame;
            frame.Navigate(typeof(Viewer), post);

            MainPage.GetMainTabView().TabItems.Add(newTab);
            MainPage.GetMainTabView().SelectedItem = newTab;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
        }
    }
}
