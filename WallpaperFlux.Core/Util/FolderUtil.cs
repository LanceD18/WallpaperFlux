﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using LanceTools.WPF.Adonis.Util;
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
        public static bool IsValidatingFolders { get; private set; } // without this RankController's verifications will bog down the system if a large number of images are disabled (from constantly setting a rank to 0)

        /*x
        public static MvxObservableCollection<FolderModel> ThemeImageFolders { get; private set; }

        
        // TODO Consider removing the reliance on this link
        // Links the ImageFolders property of WallpaperFluxViewModel to a static value,
        // allowing the ImageFolderModel to access and validate image folders (Processed on activation change)
        public static void LinkThemeImageFolders(MvxObservableCollection<FolderModel> imageFolders)
        {
            ThemeImageFolders = imageFolders;
        }
        */

        // TODO The way that the references are being handled is a bit of a mess at the moment

        // TODO Decide on if you really want to keep compatibility for multiple observable FolderModel collections,
        // TODO or just cater to the only one that's likely to exist: ImageFolders from WallpaperFluxVieWModel

        // TODO Decide on if you really want to keep compatibility for multiple observable FolderModel collections,
        // TODO or just cater to the only one that's likely to exist: ImageFolders from WallpaperFluxVieWModel

        // TODO Decide on if you really want to keep compatibility for multiple observable FolderModel collections,
        // TODO or just cater to the only one that's likely to exist: ImageFolders from WallpaperFluxVieWModel

        //? This serves a dual purpose, enabling/disabling images within a folder AND detecting new images upon validation (But for ALL folders)
        public static void ValidateImageFolders(this MvxObservableCollection<FolderModel> imageFolders, bool updateEnabledState)
        {
            IsValidatingFolders = true;

            foreach (FolderModel imageFolder in imageFolders)
            {
                imageFolder.ValidateImages(updateEnabledState);
            }

            IsValidatingFolders = false;
        }

        public static bool ContainsFolderPath(this MvxObservableCollection<FolderModel> imageFolders, string imageFolderPath)
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

        /// <summary>
        /// Uses the CommonOpenFileDialog to retrieve a valid folder path
        /// </summary>
        /// <returns>Returns string.Empty if the folder is invalid, otherwise, returns the folder path</returns>
        public static string PromptValidFolderPath()
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

                    MessageBoxUtil.ShowError("The theme does not contain this folder");
                }
            }

            return string.Empty;
        }

        public static string[] GetValidFolderPaths()
        {
            using (CommonOpenFileDialog dialog = new CommonOpenFileDialog())
            {
                // dialog properties
                dialog.Multiselect = true;
                dialog.IsFolderPicker = true;

                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    string errorOutput = "";

                    List<string> validFolders = new List<string>();
                    foreach (string fileName in dialog.FileNames)
                    {
                        if (WallpaperFluxViewModel.Instance.ContainsFolder(fileName))
                        {
                            validFolders.Add(fileName);
                        }
                        else
                        {
                            errorOutput += "\n" + fileName;
                        }
                    }

                    if (errorOutput != "")
                    {
                        MessageBoxUtil.ShowError("The theme does not contain the following folders: " + errorOutput);
                    }

                    return validFolders.ToArray();
                }
            }

            return new[] { string.Empty };
        }

        public static FolderModel PromptValidFolderModel()
        {
            string folderPath = PromptValidFolderPath();

            if (folderPath != string.Empty)
            {
                foreach (FolderModel imageFolder in WallpaperFluxViewModel.Instance.ImageFolders)
                {
                    if (imageFolder.Path == folderPath)
                    {
                        return imageFolder;
                    }
                }
            }
            
            //? We don't need an error message here because PromptValidFolderPath handles this for us

            return null;
        }

        public static FolderModel GetFolderModel(string path)
        {
            foreach (FolderModel imageFolder in WallpaperFluxViewModel.Instance.ImageFolders)
            {
                if (imageFolder.Path == path)
                {
                    return imageFolder;
                }
            }

            return null;
        }

        public static FolderModel[] GetValidFolderModels()
        {
            List<FolderModel> validFolderModels = new List<FolderModel>();
            string[] folderPaths = GetValidFolderPaths();

            foreach (string path in folderPaths)
            {
                if (path != string.Empty)
                {
                    foreach (FolderModel imageFolder in WallpaperFluxViewModel.Instance.ImageFolders)
                    {
                        if (imageFolder.Path == path)
                        {
                            validFolderModels.Add(imageFolder);
                        }
                    }
                }
            }

            //? We don't need an error message here because PromptValidFolderPath handles this for us

            return validFolderModels.ToArray();

        }
    }
}
