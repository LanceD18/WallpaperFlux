using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using LanceTools.WPF.Adonis.Util;
using MvvmCross;
using WallpaperFlux.Core.IoC;
using WallpaperFlux.Core.Models;
using WallpaperFlux.Core.Models.Controls;
using WallpaperFlux.Core.ViewModels;

namespace WallpaperFlux.Core.Util
{
    public enum RelatedImageType
    {
        None,
        Alt,
        Merge,
        Animate
    }

    public static class ImageUtil
    {
        public static Thread SetImageThread = new Thread(() => { }); // dummy null state to avoid error checking

        public static readonly string INVALID_IMAGE_SET_MESSAGE = "Mixed image types found, image set creation failed";

        public static void PromptRankImage(ImageModel image)
        {
            PromptRankImageRange(new ImageModel[] {image});
        }

        public static void PromptRankImageRange(ImageModel[] images)
        {
            if (images.Length > 1)
            {
                if (!MessageBoxUtil.PromptYesNo("Are you sure you want to rank ALL " + images.Length + " images?")) return;
            }

            MessageBoxUtil.GetPositiveInteger("Select Rank", "Enter a rank to apply", out int newRank, "Rank...");
            
            RankImageRange(images, newRank);
        }

        public static void RankImage(ImageModel image, int rank) => RankImageRange(new[] { image }, rank);

        public static void RankImageRange(ImageModel[] images, int rank)
        {
            foreach (ImageModel image in images)
            {
                //! Don't do the crossed out portion, would prevent the images from within the set from being able to be updated at all, find a better solution
                /*x
                if (image.ParentRelatedImageModel != null && !image.ParentRelatedImageModel.UsingAverageRank) //? if the image is in a set that uses an override rank, update the override rank instead
                {
                    image.ParentRelatedImageModel.OverrideRank = rank;
                }
                */
                
                image.Rank = rank; // will auto-update the rank collection in the setter
            }
        }

        public static void DeleteImage(ImageModel image)
        {
            DeleteImageRange(new ImageModel[] { image });
        }

        public static void DeleteImageRange(ImageModel[] images)
        {
            if (images.Length > 1)
            {
                if (!MessageBoxUtil.PromptYesNo("Are you sure you want to delete ALL " + images.Length + " images?" +
                                                "\n\nThis will delete the image file itself! (But properly remove it from the theme)")) return;
            }
            else //! should give a warning either way for deletion
            {
                if (!MessageBoxUtil.PromptYesNo("Are you sure you want to delete " + images[0].Path + "?" +
                                                "\n\nThis will delete the image file itself! (But properly remove it from the theme)")) return;
            }

            foreach (ImageModel image in images)
            {
                ThemeUtil.Theme.Images.RemoveImage(image);
                Mvx.IoCProvider.Resolve<IExternalFileSystemUtil>().RecycleFile(image.Path);
            }
        }

        public static void CreateRelatedImageSet(ImageModel[] images)
        {
            if (images == null) return; // likely a cancelled operation
            if (images.Length == 0) return;

            //? Having mixed image types in an image set is currently invalid, may implement in the future
            HashSet<ImageType> encounteredImageTypes = new HashSet<ImageType>();

            foreach (ImageModel image in images)
            {
                encounteredImageTypes.Add(image.ImageType);

                if (encounteredImageTypes.Count > 1)
                {
                    MessageBoxUtil.ShowError(INVALID_IMAGE_SET_MESSAGE);
                    return;
                }
            }

            // TODO Create a Related Image Set Model out of the selected images
            ImageSetModel relatedImages = new ImageSetModel(images, encounteredImageTypes.First());
            if (relatedImages.InvalidSet) return; // if the set becomes invalid, cancel the process

            // TODO Remove the selected images from the image selector
            //xImageSelectorTabModel initialTab = WallpaperFluxViewModel.Instance.GetSelectorTabOfImage(images[0]);

            ImageSelectorTabModel tabToAddTo = WallpaperFluxViewModel.Instance.GetSelectorTabOfImage(images[0]); // for use later
            WallpaperFluxViewModel.Instance.RemoveImageRangeFromTabs(images);

            // TODO Technical Debt ; Handle adjusting the image selector to account for image removal (applies to deletion as well)

            // TODO Devise a way for the individual images to no longer be accessible by the wallpaper randomization, the image set will take their place
            // TODO - This implies that we know that the given images are part of an image set, or a check is performed that finds the images existing in an image set ; Find the most optimal option

            // TODO Add the Related Image Set to the Image selector
            ThemeUtil.Theme.Images.AddImageSet(relatedImages);
            tabToAddTo.AddImage(relatedImages);
        }

        public static void AddToImageSet(ImageModel[] images, ImageSetModel targetSet)
        {
            foreach (ImageModel image in images)
            {
                if (image.ImageType != targetSet.ImageType)
                {
                    MessageBoxUtil.ShowError("Image Type mismatch between image and set, operation cancelled");
                    return;
                }
            }

            ImageSelectorTabModel targetTab = WallpaperFluxViewModel.Instance.GetSelectorTabOfImage(targetSet);

            ImageSetModel newImageSet = new ImageSetModel(targetSet.RelatedImages.Union(images).ToArray(), targetSet.ImageType);
            ReplaceImageSet(targetSet, newImageSet, targetTab);

            WallpaperFluxViewModel.Instance.RemoveImageRangeFromTabs(images);
        }

        public static void RemoveFromImageSet(ImageModel[] images, ImageSetModel targetSet)
        {
            foreach (ImageModel image in images)
            {
                if (image.ImageType != targetSet.ImageType)
                {
                    MessageBoxUtil.ShowError("Image Type mismatch between image and set, operation cancelled");
                    return;
                }
            }

            ImageSelectorTabModel targetTab = WallpaperFluxViewModel.Instance.GetSelectorTabOfImage(targetSet);

            ImageSetModel newImageSet = new ImageSetModel(targetSet.RelatedImages.Except(images).ToArray(), targetSet.ImageType);
            ReplaceImageSet(targetSet, newImageSet, targetTab);

            targetTab.AddImageRange(images);
            WallpaperFluxViewModel.Instance.RaisePropertyChanged(() => WallpaperFluxViewModel.Instance.InspectedImageSetImages);
        }

        public static void ReplaceImageSet(ImageSetModel oldImageSet, ImageSetModel newImageSet, ImageSelectorTabModel targetTab)
        {
            if (WallpaperFluxViewModel.Instance.InspectedImageSet != null && WallpaperFluxViewModel.Instance.InspectedImageSet.Equals(oldImageSet))
            {
                WallpaperFluxViewModel.Instance.InspectedImageSet = newImageSet; //? replacing removes the reference to the old image set, causing errors
            }

            ThemeUtil.Theme.Images.ReplaceImageSet(oldImageSet, newImageSet);
            targetTab.ReplaceImage(oldImageSet, newImageSet);
        }

        public static void PerformImageAction(BaseImageModel image, Action<ImageModel> action)
        {
            if (image is ImageModel imageModel)
            {
                action.Invoke(imageModel);
            }

            if (image is ImageSetModel imageSet)
            {
                foreach (ImageModel relatedImage in imageSet.RelatedImages)
                {
                    action.Invoke(relatedImage);
                }
            }
        }

        public static bool PerformImageCheck(BaseImageModel image, Func<ImageModel, bool> func)
        {
            if (image is ImageModel imageModel)
            {
                return func.Invoke(imageModel);
            }

            if (image is ImageSetModel imageSet)
            {
                foreach (ImageModel relatedImage in imageSet.RelatedImages)
                {
                    if (func.Invoke(relatedImage)) //? end on success
                    {
                        return true;
                    }
                }

                return false;
            }

            return false;
        }

        /// <summary>
        /// Returns an ImageModel from the given BaseImageModel, if a RelatedImageModel is given, the first ImageModel in the set will be returned
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public static ImageModel GetImageModel(BaseImageModel image)
        {
            if (image is ImageModel imageModel)
            {
                return imageModel;
            }

            if (image is ImageSetModel imageSet)
            {
                return imageSet.RelatedImages[0];
            }

            return null;
        }

        public static ImageModel[] GetImageSet(BaseImageModel image)
        {
            if (image is ImageModel imageModel)
            {
                return new ImageModel[] { imageModel };
            }

            if (image is ImageSetModel imageSet)
            {
                return imageSet.RelatedImages;
            }

            return null;
        }
    }
}
