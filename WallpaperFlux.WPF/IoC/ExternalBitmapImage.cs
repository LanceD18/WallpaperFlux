using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WallpaperFlux.Core.IoC;

namespace WallpaperFlux.WPF.IoC
{
    public class ExternalBitmapImage : IExternalBitmapImage
    {
        public BitmapImage ImageSource;

        public void InitCompressedSource(string imagePath, int width, int height)
        {
            ImageSource = new BitmapImage();
            ImageSource.BeginInit();
            ImageSource.UriSource = new Uri(imagePath);
            ImageSource.DecodePixelWidth = width;
            ImageSource.DecodePixelHeight = height;
            ImageSource.EndInit();

            RenderOptions.SetBitmapScalingMode(ImageSource, BitmapScalingMode.LowQuality);
        }
    }
}
