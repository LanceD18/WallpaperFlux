using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media.Imaging;

namespace WallpaperFlux.WPF.Util
{
    public static class BitmapUtil
    {
        //? bitmap images do not have a dispose method so we must manually collect
        public static void Close(this BitmapImage bitmapImage)
        {
            bitmapImage.UriSource = null;
            bitmapImage.Freeze(); // minor leaks will occur without freezing
            GC.Collect();
        }
    }
}
