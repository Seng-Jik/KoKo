using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Popups;
using Windows.UI.Xaml.Data;

namespace KoKoViewer
{
    public sealed class BrowsePostSequence : ObservableCollection<KoKoViewerPost>, ISupportIncrementalLoading
    {
        Func<IEnumerable<KoKoViewerPost>> creator;
        IEnumerator<KoKoViewerPost> iter;

        public event Action HasNoMoreNow;

        public BrowsePostSequence(Func<IEnumerable<KoKoViewerPost>> creator)
        {
            this.creator = creator;
        }

        bool hasNoMoreCalled = false;
        public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
        {
            return Task.Run(async () =>
            {
                uint actualCount = 0;
                try
                {
                    if (iter == null)
                        iter = creator().GetEnumerator();
                    for (uint i = 0; i < count; ++i)
                    {
                        HasMoreItems = iter.MoveNext();
                        if (HasMoreItems)
                        {
                            await MainPage.Get().Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
                                Add(iter.Current));
                            actualCount++;
                        }
                        else
                        {
                            if(!hasNoMoreCalled)
                                HasNoMoreNow();
                            hasNoMoreCalled = true;
                        }
                    }
                }
                catch (InvalidOperationException) { }
                catch(Exception exn)
                {
                    await MainPage.Get().Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, async () =>
                        await new MessageDialog(exn.Message).ShowAsync());
                }
                return new LoadMoreItemsResult { Count = actualCount };
            }).AsAsyncOperation<LoadMoreItemsResult>();
        }

        public bool HasMoreItems { get; private set; } = true;
    }
}
