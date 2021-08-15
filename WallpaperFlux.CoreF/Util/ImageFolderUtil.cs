using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using MvvmCross.ViewModels;
using WallpaperFlux.Core.Models;

namespace WallpaperFlux.Core.Util
{
    public static class ImageFolderUtil
    {
        public static MvxObservableCollection<ImageFolderModel> ThemeImageFolders { get; private set; }

        // TODO Consider removing the reliance on this link
        // Links the ImageFolders property of WallpaperFluxViewModel to a static value,
        // allowing the ImageFolderModel to access and validate image folders (Processed on activation change)
        public static void LinkThemeImageFolders(MvxObservableCollection<ImageFolderModel> imageFolders)
        {
            ThemeImageFolders = imageFolders;
        }

        public static bool ContainsImageFolder(this MvxObservableCollection<ImageFolderModel> imageFolders, string imageFolderPath)
        {
            foreach (ImageFolderModel imageFolder in imageFolders)
            {
                if (imageFolder.Path == imageFolderPath)
                {
                    return true;
                }
            }

            return false;
        }

        public static void ValidateImageFolders(this MvxObservableCollection<ImageFolderModel> imageFolders)
        {
            WallpaperUtil.Images.Clear();
            foreach (ImageFolderModel imageFolder in imageFolders)
            {
                if (imageFolder.Active)
                {
                    ImageModel[] images = new DirectoryInfo(imageFolder.Path).GetFiles().Select((s) => new ImageModel {Path = s.FullName}).ToArray();
                    WallpaperUtil.Images.AddRange(images);
                }
            }
        }
    }
}
