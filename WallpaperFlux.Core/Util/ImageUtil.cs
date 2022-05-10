using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace WallpaperFlux.Core.Util
{
    public static class ImageUtil
    {
        public static Thread SetImageThread = new Thread(() => { }); // dummy null state to avoid error checking
    }
}
