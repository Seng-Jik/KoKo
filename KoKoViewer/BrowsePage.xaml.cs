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
            var post = ((KoKoViewerPost)e.ClickedItem).post;

            var newTab = new TabViewItem();
            newTab.IconSource = new SymbolIconSource() { Symbol = Symbol.Pictures };
            newTab.Header = post.fromSpider.Name + " " + post.id;

            // The Content of a TabViewItem is often a frame which hosts a page.
            Frame frame = new Frame();
            newTab.Content = frame;
            frame.Navigate(typeof(Viewer), Tuple.Create(post, searchOption));


            MainPage.Get().InsertTabViewAfterCurrent(newTab);
        }
    }
}
