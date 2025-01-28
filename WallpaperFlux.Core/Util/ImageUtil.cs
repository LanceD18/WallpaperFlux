using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using AdonisUI.Controls;
using LanceTools.WPF.Adonis.Util;
using MvvmCross;
using WallpaperFlux.Core.IoC;
using WallpaperFlux.Core.Models;
using WallpaperFlux.Core.Models.Controls;
using WallpaperFlux.Core.ViewModels;

namespace WallpaperFlux.Core.Util
{
    public enum ImageSetType
    {
        Alt,
        Animate,
        Merge
    }

    public enum ImageSetRankingFormat
    {
        Average,
        WeightedAverage,
        Override,
        WeightedOverride
    }

    public static class ImageUtil
    {
        public static Thread SetImageThread = new Thread(() => { }); // dummy null state to avoid error checking

        public static readonly string INVALID_IMAGE_SET_MESSAGE = "A set with mixed image types (Static/Gif/Video) is not yet supported";

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
                if (image.ParentImageSet != null && !image.ParentImageSet.UsingAverageRank) //? if the image is in a set that uses an override rank, update the override rank instead
                {
                    image.ParentImageSet.OverrideRank = rank;
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

        public static void PerformImageAction(BaseImageModel image, Action<ImageModel> action, bool checkForEnabled = true)
        {
            switch (image)
            {
                case ImageModel imageModel:
                    if (checkForEnabled && !image.IsEnabled(true)) return;

                    action.Invoke(imageModel);
                    break;

                case ImageSetModel imageSet:
                {
                    foreach (ImageModel relatedImage in imageSet.GetRelatedImages(checkForEnabled))
                    {
                        action.Invoke(relatedImage);
                    }

                    break;
                }
            }
        }

        public static bool PerformImageCheck(BaseImageModel image, Func<ImageModel, bool> func, bool checkForEnabled = true)
        {
            switch (image)
            {
                case ImageModel imageModel:
                    if (checkForEnabled && !image.IsEnabled(true)) return false;

                    return func.Invoke(imageModel);

                case ImageSetModel imageSet:
                {
                    foreach (ImageModel relatedImage in imageSet.GetRelatedImages(checkForEnabled))
                    {
                        if (func.Invoke(relatedImage)) //? end on success
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns an ImageModel from the given BaseImageModel, if a RelatedImageModel is given, the first ImageModel in the set will be returned
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public static ImageModel GetImageModel(BaseImageModel image, bool checkForEnabled = true)
        {
            switch (image)
            {
                case ImageModel imageModel:
                    return imageModel;

                case ImageSetModel imageSet:
                {
                    ImageModel[] images = imageSet.GetRelatedImages(checkForEnabled);

                    if (images.Length > 0)
                    {
                        return images[0];
                    }

                    break;
                }
            }

            return null;
        }

        public static ImageModel[] GetImageSet(BaseImageModel image, bool checkForEnabled = true)
        {
            switch (image)
            {
                case ImageModel imageModel:
                    return new ImageModel[] { imageModel };

                case ImageSetModel imageSet:
                    return imageSet.GetRelatedImages(checkForEnabled);
            }

            return null;
        }


        private const string DISPLAY_DEFAULT_ID = "display";
        public static void SetWallpaper(BaseImageModel image)
        {
            int displayIndex = 0;
            if (WallpaperUtil.DisplayUtil.GetDisplayCount() > 1) // this MessageBox will only appear if the user has more than one display
            {
                // Create [Choose Display] MessageBox
                IMessageBoxButtonModel[] buttons = new IMessageBoxButtonModel[WallpaperUtil.DisplayUtil.GetDisplayCount()];
                for (int i = 0; i < buttons.Length; i++)
                {
                    buttons[i] = MessageBoxButtons.Custom("Display " + (i + 1), DISPLAY_DEFAULT_ID + i);
                }

                MessageBoxModel messageBox = new MessageBoxModel
                {
                    Text = "Choose a display",
                    Caption = "Choose an option",
                    Icon = MessageBoxImage.Question,
                    Buttons = buttons
                };

                // Display [Choose Display] MessageBox
                MessageBox.Show(messageBox);

                // Evaluate [Choose Display] MessageBox
                for (int i = 0; i < buttons.Length; i++)
                {
                    if ((string)messageBox.ButtonPressed.Id == (DISPLAY_DEFAULT_ID + i))
                    {
                        displayIndex = i;
                        break;
                    }
                }
            }

            WallpaperUtil.SetWallpaper(displayIndex, true, true, image); // no randomization required here
        }
    }
}
