using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using SymbolIconSource = Microsoft.UI.Xaml.Controls.SymbolIconSource;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace KoKoViewer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class HomePage : Page
    {
        TabViewItem tabViewItem;
        public HomePage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            tabViewItem = e.Parameter as TabViewItem;

            Nav.SelectedItem = Nav_Search;
        }

        private void Nav_SelectionChanged(Microsoft.UI.Xaml.Controls.NavigationView sender, Microsoft.UI.Xaml.Controls.NavigationViewSelectionChangedEventArgs args)
        {
            if (Nav_Search.IsSelected)
            {
                MainFrame.Navigate(typeof(SearchPage), tabViewItem);
                tabViewItem.Header = "Search";
                tabViewItem.IconSource = new SymbolIconSource() { Symbol = Symbol.Find };
            }

            else if(Nav_Favorites.IsSelected)
            {
                tabViewItem.Header = "Favorites";
                tabViewItem.IconSource = new SymbolIconSource() { Symbol = Symbol.Favorite };

                var searchOption = new SearchOption()
                {
                    Safe = true,
                    Questionable = true,
                    Explicit = true,
                    Unknown = true,
                    Spiders = KoKo.AllSpiders.AllSpiders.ToList()
                };
                var param = Tuple.Create(FavoritesData.Get().GetAllFavoritesSequence() as ObservableCollection<KoKoViewerPost>, searchOption);
                MainFrame.Navigate(typeof(BrowsePage), param);
            }
            //FavoritesFrame.Navigate(typeof(BrowsePage));
        }
    }
}
