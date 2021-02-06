using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;
using Microsoft.Toolkit.Uwp.Notifications;
using System.Threading;

namespace KoKoViewer
{
    static class DownloadHelper
    {
        static ToastNotifier notifier = ToastNotificationManager.CreateToastNotifier();
        static void ToastFinished(KoKo.Post post, string path)
        {
            string title = "KoKo Viewer";
            string content = $"{post.fromSpider.Name} {post.id} Downloaded.";
            string image = "file://" + path;

            if (path.ToLower().EndsWith(".mp4") || path.ToLower().EndsWith(".webm"))
                image = post.previewImage.Value.imageUrl;

            string visualXml = $@"
            <visual><binding template='ToastGeneric'><image src='{image}' /><text>{title}</text><text>{content}</text></binding></visual>";

            string toastXml = $@"<toast>{visualXml}</toast>";

            XmlDocument xml = new XmlDocument();
            xml.LoadXml(toastXml);
            var toast = new ToastNotification(xml);

            notifier.Show(toast);
        }

        static void ToastFailed(KoKo.Post post, string message)
        {
            string title = "KoKo Viewer";
            string content = $"{post.fromSpider.Name} {post.id} Download failed: {message}";
            string image = post.previewImage.Value.imageUrl;

            string visualXml = $@"
            <visual><binding template='ToastGeneric'><image src='{image}' /><text>{title}</text><text>{content}</text></binding></visual>";

            string toastXml = $@"<toast>{visualXml}</toast>";

            XmlDocument xml = new XmlDocument();
            xml.LoadXml(toastXml);
            var toast = new ToastNotification(xml);

            notifier.Show(toast);
        }

        static ToastNotification ToastProgress(KoKo.Post post)
        {
            var content = new ToastContentBuilder()
                .AddText("KoKo Viewer")
                .AddInlineImage(new Uri(post.previewImage.Value.imageUrl))
                .AddVisualChild(new AdaptiveProgressBar()
                {
                    Title = $"{post.fromSpider.Name} {post.id}",
                    Value = new BindableProgressBarValue("progressValue"),
                    Status = "Downloading..."
                })
                .GetToastContent();

            var toast = new ToastNotification(content.GetXml());
            toast.Data = new NotificationData();
            toast.Tag = $"{post.fromSpider.Name} {post.id}";
            notifier.Show(toast);

            return toast;
        }

        public static StorageFile GetDownloaded(KoKo.Post post)
        {
            StorageFolder applicationFolder = KnownFolders.PicturesLibrary;
            StorageFolder folder = applicationFolder.CreateFolderAsync("KoKo Images", CreationCollisionOption.OpenIfExists).AsTask().Result;
            var image = post.images.First().First();

            try
            {
                var a = folder.GetFileAsync(KoKo.Utils.normalizeFileName(image.fileName)).AsTask().Result;
                var size = a.GetBasicPropertiesAsync().AsTask().Result.Size;
                return size > 0 ? a : null;
            }
            catch(Exception)
            {
                return null;
            }
        }

        public static async void Download(KoKo.Post post)
        {
            StorageFolder applicationFolder = KnownFolders.PicturesLibrary;
            StorageFolder folder = await applicationFolder.CreateFolderAsync("KoKo Images", CreationCollisionOption.OpenIfExists);
            var image = post.images.First().First();
            var path = await folder.CreateFileAsync(KoKo.Utils.normalizeFileName(image.fileName), CreationCollisionOption.ReplaceExisting);

            try
            {
                var client = new System.Net.WebClient();

                var toast = ToastProgress(post);
                client.DownloadProgressChanged += (ooo, eee) =>
                {
                    MainPage.Get().Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        toast.Data.Values["progressValue"] = ((double)eee.BytesReceived / (double)eee.TotalBytesToReceive).ToString();
                        notifier.Update(toast.Data, toast.Tag);
                    }).AsTask().Wait();
                };

                client.DownloadDataCompleted += async (ooo2, eee2) =>
                {
                    using (var file = await path.OpenAsync(FileAccessMode.ReadWrite))
                    {
                        await file.WriteAsync(eee2.Result.AsBuffer());
                        await file.FlushAsync();
                    }

                    notifier.Hide(toast);
                    ToastFinished(post, path.Path);

                    (ooo2 as IDisposable).Dispose();
                };

                new Thread(() => client.DownloadDataAsync(new Uri(image.imageUrl))).Start();
            }
            catch(Exception e)
            {
                ToastFailed(post, e.Message);
            }
        }
    }
}
