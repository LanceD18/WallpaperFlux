using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using MvvmCross;
using MvvmCross.ViewModels;
using WallpaperFlux.Core.External;
using WallpaperFlux.Core.Models;
using WallpaperFlux.Core.Models.Theme;

namespace WallpaperFlux.Core.Util
{
    public static class FolderUtil
    {
        public static MvxObservableCollection<FolderModel> ThemeImageFolders { get; private set; }

        // TODO Consider removing the reliance on this link
        // Links the ImageFolders property of WallpaperFluxViewModel to a static value,
        // allowing the ImageFolderModel to access and validate image folders (Processed on activation change)
        public static void LinkThemeImageFolders(MvxObservableCollection<FolderModel> imageFolders)
        {
            ThemeImageFolders = imageFolders;
        }

        public static bool ContainsImageFolder(this MvxObservableCollection<FolderModel> imageFolders, string imageFolderPath)
        {
            foreach (FolderModel imageFolder in imageFolders)
            {
                if (imageFolder.Path == imageFolderPath)
                {
                    return true;
                }
            }

            return false;
        }

        public static void ValidateImageFolders(this MvxObservableCollection<FolderModel> imageFolders)
        {
            foreach (FolderModel imageFolder in imageFolders)
            {
                ValidateImageFolder(imageFolder);
            }
        }

        public static void ValidateImageFolder(this FolderModel imageFolder)
        {
            imageFolder.ValidateImages();
        }
    }
}
