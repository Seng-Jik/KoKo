using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using SymbolIconSource = Microsoft.UI.Xaml.Controls.SymbolIconSource;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace KoKoViewer
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        static MainPage page;
        public MainPage()
        {
            page = this;
            this.InitializeComponent();

            var titleBar = CoreApplication.GetCurrentView().TitleBar;
            titleBar.ExtendViewIntoTitleBar = true;
            titleBar.LayoutMetricsChanged += CoreTitleBar_LayoutMetricsChanged;
            Window.Current.SetTitleBar(CustomDragRegion);
            
            var titleBar2 = ApplicationView.GetForCurrentView().TitleBar; 
            titleBar2.ButtonBackgroundColor = Colors.Transparent;
            titleBar2.ButtonInactiveBackgroundColor = Colors.Transparent;
        }

        public static MainPage Get()
        {
            return page;
        }

        public static TabView GetMainTabView()
        {
            return page.MainTabView;
        }

        public void InsertTabViewAfterCurrent(TabViewItem item, bool jumpTo = true)
        {
            try
            {
                MainTabView.TabItems.Insert(MainTabView.SelectedIndex + 1, item);
            }
            catch(Exception)
            {
                MainTabView.TabItems.Add(item);
            }

            if(jumpTo)
                MainPage.GetMainTabView().SelectedItem = item;
        }

        private void CoreTitleBar_LayoutMetricsChanged(CoreApplicationViewTitleBar sender, object args)
        {
            if (FlowDirection == FlowDirection.LeftToRight)
            {
                CustomDragRegion.MinWidth = sender.SystemOverlayRightInset;
                ShellTitlebarInset.MinWidth = sender.SystemOverlayLeftInset;
            }
            else
            {
                CustomDragRegion.MinWidth = sender.SystemOverlayLeftInset;
                ShellTitlebarInset.MinWidth = sender.SystemOverlayRightInset;
            }

            CustomDragRegion.Height = ShellTitlebarInset.Height = sender.Height;
        }

        public void NewPage(string initSearchKeyword)
        {
            var newTab = new TabViewItem();

            // The Content of a TabViewItem is often a frame which hosts a page.
            Frame frame = new Frame();
            newTab.Content = frame;
            frame.Navigate(typeof(HomePage), Tuple.Create(newTab, initSearchKeyword));

            MainTabView.TabItems.Add(newTab);
            MainTabView.SelectedItem = newTab;
        }

        // Remove the requested tab from the TabView
        private void ClosePage(TabView sender, TabViewTabCloseRequestedEventArgs args)
        {
            sender.TabItems.Remove(args.Tab);

            if (sender.TabItems.Count == 0)
                CoreApplication.Exit();
        }

        private void TabView_Loaded(object sender, RoutedEventArgs e)
        {
            NewPage("");
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            MainTabView.Width = ActualWidth;
            MainTabView.Height = ActualHeight;
        }

        private void MainTabView_AddTabButtonClick(TabView sender, object args)
        {
            NewPage("");
        }
    }
}
