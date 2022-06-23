using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.WindowsAPICodePack.Dialogs;
using MvvmCross;
using MvvmCross.ViewModels;
using WallpaperFlux.Core.Models;
using WallpaperFlux.Core.Models.Theme;
using WallpaperFlux.Core.ViewModels;

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

        //? This serves a dual purpose, enabling/disabling images within a folder AND detecting new images upon validation (But for ALL folders)
        public static void ValidateImageFolders(this MvxObservableCollection<FolderModel> imageFolders)
        {
            foreach (FolderModel imageFolder in imageFolders)
            {
                imageFolder.ValidateImages();
            }
        }

        /// <summary>
        /// Uses the CommonOpenFileDialog to retrieve a valid folder path
        /// </summary>
        /// <returns>Returns string.Empty if the folder is invalid, otherwise, returns the folder path</returns>
        public static string GetValidFolderPath()
        {
            using (CommonOpenFileDialog dialog = new CommonOpenFileDialog())
            {
                // dialog properties
                dialog.Multiselect = false;
                dialog.IsFolderPicker = true;

                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    if (WallpaperFluxViewModel.Instance.ContainsFolder(dialog.FileName))
                    {
                        return dialog.FileName;
                    }
                    else
                    {
                        MessageBoxUtil.ShowError("The theme does not contain this folder");
                    }
                }
            }

            return string.Empty;
        }
    }
}
