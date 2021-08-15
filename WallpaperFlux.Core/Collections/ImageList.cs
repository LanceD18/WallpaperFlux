using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MvvmCross;
using WallpaperFlux.Core.External;
using WallpaperFlux.Core.Models;

namespace WallpaperFlux.Core.Collections
{
    public class ImageList
    {
        // TODO When activating images, ImageFolders should be immediately taken into account

        private Dictionary<string, ImageModel> ImageContainer = new Dictionary<string, ImageModel>();
        private Dictionary<int, string> RankData = new Dictionary<int, string>();

        //private Reactive

        public ImageModel AddImage(string path) => AddImage(GetImage(path));

        public ImageModel[] AddImages(string[] paths) => AddImages(GetImages(paths));

        public ImageModel AddImage(ImageModel path)
        {
            throw new NotImplementedException();
        }

        public ImageModel[] AddImages(ImageModel[] paths)
        {
            throw new NotImplementedException();
        }

        public ImageModel[] AddImagesOfDirectory(string directory)
        {
            ImageModel[] images = new DirectoryInfo(directory).GetFiles().Select((s) =>
                new ImageModel(Mvx.IoCProvider.Resolve<IExternalImageSource>()) { Path = s.FullName }).ToArray();

            return images;
        }

        //? will process procedure needed to add an image with no ImageModel, should be called by AddImage(s)
        public ImageModel CreateImage()
        {
            throw new NotImplementedException();
        }

        public bool RemoveImage(string path) => RemoveImage(GetImage(path));

        public bool RemoveImages(string[] paths, out string[] failedRemovals) => RemoveImages(GetImages(paths), out failedRemovals);

        public bool RemoveImage(ImageModel path)
        {
            throw new NotImplementedException();
        }

        public bool RemoveImages(ImageModel[] paths, out string[] failedRemovals)
        {
            // TODO return false if any removal fails and send out the failed removals
            throw new NotImplementedException();
        }

        public bool ContainsImage(string path) => GetImage(path) == null;

        public ImageModel GetImage(string path)
        {
            throw new NotImplementedException();
        }

        public ImageModel[] GetImages(string[] paths)
        {
            throw new NotImplementedException();
        }

        public ImageModel[] GetImages(string directory)
        {
            throw new NotImplementedException();
        }

        public ImageModel[] GetImagesOfRank(int rank)
        {
            throw new NotImplementedException();
        }

        public ImageModel[] GetImagesOfRanks(int[] ranks)
        {
            throw new NotImplementedException();
        }

        public ImageModel[] GetAllRankedImages()
        {
            throw new NotImplementedException();
        }

        public ImageModel[] GetAllImages()
        {
            throw new NotImplementedException();
        }
    }
}
