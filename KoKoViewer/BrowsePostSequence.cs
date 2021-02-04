using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Data;

namespace KoKoViewer
{
    public sealed class BrowsePostSequence : ObservableCollection<KoKoViewerPost>, ISupportIncrementalLoading
    {
        Func<IEnumerable<KoKoViewerPost>> creator;
        IEnumerator<KoKoViewerPost> iter;

        public BrowsePostSequence(Func<IEnumerable<KoKoViewerPost>> creator)
        {
            this.creator = creator;
        }

        public IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
        {
            return Task.Run(async () =>
            {
                if (iter == null)
                    iter = creator().GetEnumerator();
                uint actualCount = 0;
                for(uint i = 0; i < count;++i)
                {
                    HasMoreItems = iter.MoveNext();
                    if(HasMoreItems)
                    {
                        await MainPage.Get().Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
                            Add(iter.Current));
                        actualCount++;
                    }
                }
                return new LoadMoreItemsResult { Count = actualCount };
            }).AsAsyncOperation<LoadMoreItemsResult>();
        }

        public bool HasMoreItems { get; private set; } = true;
    }
}
