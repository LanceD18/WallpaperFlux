using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using LanceTools.IO;
using WallpaperFlux.Core.IoC;
using WallpaperFlux.Core.Util;

namespace WallpaperFlux.WPF.IoC
{
    // TODO Consider updating this to where instead of having an _internalImage we just inherit directly from Image ; would have to check that SetImage() still works
    // TODO Consider updating this to where instead of having an _internalImage we just inherit directly from Image ; would have to check that SetImage() still works
    // TODO Consider updating this to where instead of having an _internalImage we just inherit directly from Image ; would have to check that SetImage() still works
    public class ExternalImage : IExternalImage
    {
        private Image _internalImage;

        private readonly string PATH_NOT_SET_ERROR = "ERROR: An ExternalImage was created but the Image path was never set, fix this";

        public bool SetImage(string imagePath) //! the return value is not being used at the moment, consider removing
        {
            if (ImageUtil.SetImageThread.IsAlive) ImageUtil.SetImageThread.Join();

            bool success = false;

            ImageUtil.SetImageThread = new Thread(() =>
            {
                if (_internalImage == null)
                {
                    if (FileUtil.Exists(imagePath) && !WallpaperUtil.IsSupportedVideoType(imagePath))
                    {
                        try
                        {
                            _internalImage = Image.FromFile(imagePath);
                            success = true;
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine(e);
                            success = false;
                        }

                    }
                }
                else // no need to do anything if the image already exists
                {
                    success = true;
                }

                success = false;
            });
            ImageUtil.SetImageThread.Start();
            ImageUtil.SetImageThread.Join();

            return success;
        }

        public Size GetSize()
        {
            if (ImageUtil.SetImageThread.IsAlive) ImageUtil.SetImageThread.Join();

            if (_internalImage != null)
            {
                return _internalImage.Size;
            }
            else
            {
                Debug.WriteLine(PATH_NOT_SET_ERROR);
                return Size.Empty;
            }
        }

        public object GetTag()
        {
            if (ImageUtil.SetImageThread.IsAlive) ImageUtil.SetImageThread.Join();

            if (_internalImage != null)
            {
                return _internalImage.Tag;
            }
            else
            {
                Debug.WriteLine(PATH_NOT_SET_ERROR);
                return null;
            }
        }

        public void SetTag(object tag)
        {
            if (ImageUtil.SetImageThread.IsAlive) ImageUtil.SetImageThread.Join();

            if (_internalImage != null)
            {
                _internalImage.Tag = tag;
            }
            else
            {
                Debug.WriteLine(PATH_NOT_SET_ERROR);
            }
        }

        public void Dispose()
        {
            if (ImageUtil.SetImageThread.IsAlive) ImageUtil.SetImageThread.Join();

            if (_internalImage != null)
            {
                _internalImage.Dispose();
            }
            else
            {
                Debug.WriteLine(PATH_NOT_SET_ERROR);
            }
        }
    }
}
