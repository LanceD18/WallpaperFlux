using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using WallpaperFlux.Core.External;
using WallpaperFlux.Core.Util;

namespace WallpaperFlux.WPF.External
{
    public class ExternalImage : IExternalImage
    {
        private Image internalImage;

        public bool SetImage(string imagePath)
        {
            if (File.Exists(imagePath) && !WallpaperUtil.IsSupportedVideoType(imagePath))
            {
                internalImage = Image.FromFile(imagePath);
                return true;
            }

            return false;
        }

        public Size GetSize() => internalImage.Size;

        public object GetTag() => internalImage.Tag;

        public void SetTag(object tag) => internalImage.Tag = tag;

        public void Dispose() => internalImage.Dispose();
    }
}
