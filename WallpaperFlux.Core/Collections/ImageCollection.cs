using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using MvvmCross;
using WallpaperFlux.Core.External;
using WallpaperFlux.Core.Models;
using WallpaperFlux.Core.Util;

namespace WallpaperFlux.Core.Collections
{
    public class ImageCollection
    {
        private Dictionary<ImageType, Dictionary<string, ImageModel>> ImageContainer = new Dictionary<ImageType, Dictionary<string, ImageModel>>()
        {
            {ImageType.Static, new Dictionary<string, ImageModel>()},
            {ImageType.GIF, new Dictionary<string, ImageModel>()},
            {ImageType.Video, new Dictionary<string, ImageModel>()}
        };

        public ImageModel AddImage(string path)
        {
            if (ContainsImage(path)) return null;

            ImageModel addedImage = new ImageModel(path);
            ImageContainer[addedImage.ImageType].Add(path, addedImage);
            return addedImage;
        }

        public ImageModel[] AddImageRange(string[] paths)
        {
            List<ImageModel> images = new List<ImageModel>();
            foreach (string path in paths)
            {
                if (ContainsImage(path)) continue;

                images.Add(AddImage(path));
            }

            return images.ToArray();
        }

        public void AddImage(ImageModel image)
        {
            if (ContainsImage(image.Path)) return;

            ImageContainer[image.ImageType].Add(image.Path, image);
        }

        public void AddImageRange(ImageModel[] images)
        {
            foreach (ImageModel image in images)
            {
                if (ContainsImage(image.Path)) continue;
                AddImage(image);
            }
        }

        public ImageModel[] AddImagesOfDirectory(string directory)
        {
            ImageModel[] images = new DirectoryInfo(directory).GetFiles().Select((s) =>
                new ImageModel(s.FullName)).ToArray();

            return images;
        }

        //? Keep in mind that all Remove methods trace back to this method, so sweeping changes that should apply to all of them should be placed here
        public bool RemoveImage(ImageModel image)
        {
            DataUtil.Theme.RankController.RemoveRankedImage(image, this);

            return ImageContainer[image.ImageType].Remove(image.Path);
        }

        public bool RemoveImageRange(ImageModel[] images, out string[] failedRemovals)
        {
            List<string> failed = new List<string>();
            foreach (ImageModel image in images)
            {
                if (!RemoveImage(image))
                {
                    failed.Add(image.Path);
                }
            }

            failedRemovals = failed.ToArray();

            return failedRemovals.Length == 0;
        }

        public bool RemoveImage(string path)
        {
            return RemoveImage((GetImage(path)));
        }

        public bool RemoveImageRange(string[] paths, out string[] failedRemovals)
        {
            List<string> failed = new List<string>();
            foreach (string path in paths)
            {
                if (!RemoveImage(path))
                {
                    failed.Add(path);
                }
            }

            failedRemovals = failed.ToArray();

            return failedRemovals.Length == 0;
        }

        public bool ContainsImage(string path, ImageType imageType) => ImageContainer[imageType].ContainsKey(path);

        public bool ContainsImage(string path) => GetImage(path) != null;

        public bool ContainsImage(ImageModel image) => ImageContainer[image.ImageType].ContainsValue(image);

        public ImageModel GetImage(string path)
        {
            foreach (ImageType imageType in ImageContainer.Keys)
            {
                if (ContainsImage(path, imageType))
                {
                    return ImageContainer[imageType][path];
                }
            }

            return null; // image not found
        }

        public ImageModel[] GetImageRange(string[] paths)
        {
            List<ImageModel> images = new List<ImageModel>();
            foreach (string path in paths)
            {
                if (!ContainsImage(path)) continue;

                images.Add((GetImage(path)));
            }

            return images.ToArray();
        }

        public string[] GetAllImagePaths(ImageType imageType) => ImageContainer[imageType].Keys.ToArray();

        public string[] GetAllImagePaths()
        {
            List<string> imagePaths = new List<string>();
            foreach (ImageType imageType in ImageContainer.Keys)
            {
                imagePaths.AddRange(GetAllImagePaths(imageType));
            }

            return imagePaths.ToArray();
        }

        public ImageModel[] GetAllImages(ImageType imageType) => ImageContainer[imageType].Values.ToArray();

        public ImageModel[] GetAllImages()
        {
            List<ImageModel> imagePaths = new List<ImageModel>();
            foreach (ImageType imageType in ImageContainer.Keys)
            {
                imagePaths.AddRange(GetAllImages(imageType));
            }

            return imagePaths.ToArray();
        }
    }
}
