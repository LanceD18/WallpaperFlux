using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WallpaperFlux.Core.External;

namespace WallpaperFlux.WPF.External
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
