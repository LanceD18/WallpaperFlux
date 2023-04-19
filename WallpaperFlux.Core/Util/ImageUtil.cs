using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using LanceTools.WPF.Adonis.Util;
using MvvmCross;
using WallpaperFlux.Core.IoC;
using WallpaperFlux.Core.Models;

namespace WallpaperFlux.Core.Util
{
    public enum RelatedImageType
    {
        AltRanker,
        Merger,
        Animator
    }

    public static class ImageUtil
    {
        public static Thread SetImageThread = new Thread(() => { }); // dummy null state to avoid error checking

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
            foreach (ImageModel image in images) image.Rank = rank; // will auto-update the rank collection in the setter
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
    }
}
