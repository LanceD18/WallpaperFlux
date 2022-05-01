using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        private readonly string PATH_NOT_SET_ERROR = "ERROR: An ExternalImage was created but the Image path was never set, fix this";

        public bool SetImage(string imagePath)
        {
            if (File.Exists(imagePath) && !WallpaperUtil.IsSupportedVideoType(imagePath))
            {
                internalImage = Image.FromFile(imagePath);
                return true;
            }

            return false;
        }

        public Size GetSize()
        {
            try
            {
                return internalImage.Size;
            }
            catch (Exception e)
            {
                Debug.WriteLine("PATH_NOT_SET_ERROR");
                throw;
            }
        }

        public object GetTag()
        {
            try
            {
                return internalImage.Tag;
            }
            catch (Exception e)
            {
                Debug.WriteLine("PATH_NOT_SET_ERROR");
                throw;
            }
        }

        public void SetTag(object tag)
        {
            try
            {
                internalImage.Tag = tag;
            }
            catch (Exception e)
            {
                Debug.WriteLine("PATH_NOT_SET_ERROR");
                throw;
            }
        }

        public void Dispose()
        {
            try
            {
                internalImage.Dispose();
            }
            catch (Exception e)
            {
                Debug.WriteLine("PATH_NOT_SET_ERROR");
                throw;
            }
        }
    }
}
