using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using LanceTools;
using MvvmCross;
using WallpaperFlux.Core.Models;
using WallpaperFlux.Core.Util;
using WallpaperFlux.Core.ViewModels;

namespace WallpaperFlux.Core.Collections
{
    // TODO Might rename to ImageController since there's only one instance of this
    public class ImageCollection
    {
        // [ImageType, [Path, ImageModel] ; Allows us to either use the string path to reference an image or a value method such as ContainsValue()
        private Dictionary<ImageType, Dictionary<string, ImageModel>> ImageContainer = new Dictionary<ImageType, Dictionary<string, ImageModel>>()
        {
            {ImageType.Static, new Dictionary<string, ImageModel>()},
            {ImageType.GIF, new Dictionary<string, ImageModel>()},
            {ImageType.Video, new Dictionary<string, ImageModel>()}
        };

        private HashSet<ImageSetModel> ImageSets = new HashSet<ImageSetModel>();

        // TODO Consider removing this Action
        public Action<ImageModel> OnRemove; //? If you need multiple OnRemove events, use/re-purpose the delegate format used by ReactiveList

        //x public delegate void ImageCollectionChanged(object sender, ImageCollectionChangedEventArgs e);
        //x public event ImageCollectionChanged OnListRemoveItem;
        
        public ImageModel AddImage(string path, FolderModel parentFolder)
        {
            if (ContainsImage(path)) return null;

            ImageModel addedImage = new ImageModel(path, volume: ThemeUtil.VideoSettings.DefaultVideoVolume);
            AddImage(addedImage, parentFolder);
            return addedImage;
        }

        public ImageModel[] AddImageRange(string[] paths, FolderModel parentFolder)
        {
            List<ImageModel> images = new List<ImageModel>();
            foreach (string path in paths)
            {
                ImageModel newImage = AddImage(path, parentFolder);

                if (newImage != null) images.Add(newImage);
            }

            return images.ToArray();
        }

        public void AddImage(ImageModel image, FolderModel parentFolder)
        {
            //xif (ContainsImage(image)) return;
            if (ImageContainer[image.ImageType].ContainsKey(image.Path)) return; //? an image with the same path may not necessarily have the same object, can occur on re-load

            ImageContainer[image.ImageType].Add(image.Path, image);

            image.ParentFolder = parentFolder;
        }

        public void AddImageRange(ImageModel[] images, FolderModel parentFolder)
        {
            foreach (ImageModel image in images)
            {
                AddImage(image, parentFolder);
            }
        }

        public ImageModel[] AddImagesOfDirectory(string directory)
        {
            ImageModel[] images = new DirectoryInfo(directory).GetFiles().Select((s) =>
                new ImageModel(s.FullName, volume: ThemeUtil.VideoSettings.DefaultVideoVolume)).ToArray();

            return images;
        }

        public ImageSetModel[] GetAllImageSets()
        {
            return ImageSets.ToArray();
        }

        public void AddImageSet(ImageSetModel imageSet)
        {
            ImageSets.Add(imageSet);
        }

        public void ReplaceImageSet(ImageSetModel oldImageSet, ImageSetModel newImageSet)
        {
            ImageSets.Remove(oldImageSet);
            ImageSets.Add(newImageSet);
        }

        public void UpdateImageCollectionPath(ImageModel image, string oldPath, string newPath)
        {
            // TODO YOU FORGOT TO UPDATE THE RANK CONTROLLER (Might want to do this in the ImageModel instead)
            ImageContainer[image.ImageType].Remove(oldPath);
            ImageContainer[image.ImageType].Add(newPath, image);

            image.UpdateParentFolder();
        }

        //? Keep in mind that all Remove methods trace back to this method, so sweeping changes that should apply to all of them should be placed here
        public bool RemoveImage(ImageModel image)
        {
            ThemeUtil.Theme.RankController.RemoveRankedImage(image, true);

            image.RemoveAllTags();
            WallpaperFluxViewModel.Instance.SelectedImageSelectorTab.RemoveImage(image);

            if (image.IsInRelatedImageSet)
            {
                ImageUtil.RemoveFromImageSet(new ImageModel[] {image}, image.ParentRelatedImageModel);
            }

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
            return RemoveImage(GetImage(path));
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

        public bool RemoveSet(ImageSetModel set)
        {
            return ImageSets.Remove(set);
        }

        public bool ContainsImage(string path, ImageType imageType)
        {
            if (string.IsNullOrEmpty(path)) return false;
            return ImageContainer[imageType].ContainsKey(path);
        }

        public bool ContainsImage(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            return GetImage(path) != null;
        }

        public bool ContainsImage(BaseImageModel image)
        {
            if (image is ImageModel imageModel)
            {
                //! remember that searching for keys is significantly faster so if possible search that way instead
                //! remember that searching for keys is significantly faster so if possible search that way instead
                return ContainsImage(imageModel.Path, imageModel.ImageType); 
            }

            if (image is ImageSetModel relatedImageModel)
            {
                return ContainsImage(relatedImageModel);
            }

            return false;
        }

        public bool ContainsImage(ImageModel image)
        {
            //! remember that searching for keys is significantly faster so if possible search that way instead
            //! remember that searching for keys is significantly faster so if possible search that way instead
            return ContainsImage(image.Path, image.ImageType);
        }

        public bool ContainsImage(ImageSetModel image) => ImageSets.Contains(image);

        public ImageModel GetImage(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

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

                images.Add(GetImage(path));
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
            List<ImageModel> images = new List<ImageModel>();
            foreach (ImageType imageType in ImageContainer.Keys)
            {
                images.AddRange(GetAllImages(imageType));
            }

            return images.ToArray();
        }

        public int GetEnabledImagesInSetsCount()
        {
            int count = 0;

            foreach (ImageSetModel set in GetAllImageSets())
            {
                int setCount = set.RelatedImages.Length;

                foreach (ImageModel image in set.RelatedImages)
                {
                    if (!image.IsEnabled(true))
                    {
                        setCount--;
                    }
                }

                count += setCount;
            }

            return count;
        }

        //? RankController classifies images by images type by default, so performing this action there is much easier
        public BaseImageModel[] GetAllImagesOfType(ImageType imageType) => ThemeUtil.Theme.RankController.GetAllImagesOfType(imageType);
    }
}
