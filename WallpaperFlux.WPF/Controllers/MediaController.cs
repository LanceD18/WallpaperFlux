using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Unosquare.FFME;

namespace WallpaperFlux.WPF.Controllers
{
    public static class MediaController
    {
        private static Queue<(MediaElement, string)> Requests = new Queue<(MediaElement, string)>();

        public static void SendRequest(MediaElement mediaElement, string mediaPath)
        {
            Requests.Enqueue((mediaElement, mediaPath));

            if (Requests.Count == 1) ProcessRequests();
        }

        private static async void ProcessRequests()
        {
            await Task.Run(() =>
            {
                while (Requests.Count > 0)
                {
                    Debug.WriteLine(Requests.Count);
                    (MediaElement, string) mediaInfo = Requests.Dequeue();
                    mediaInfo.Item1.Open(new Uri(mediaInfo.Item2));
                }
            }).ConfigureAwait(false);
        }
    }
}
