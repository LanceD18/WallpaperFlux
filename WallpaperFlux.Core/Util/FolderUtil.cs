using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        //? This serves a dual purpose, enabling/disabling images within a folder AND detecting new images upon validation (But for ALL folders)
        public static void ValidateImageFolders(this MvxObservableCollection<FolderModel> imageFolders, bool updateEnabledState)
        {
            //? asyncing this has the potential to cause issues if the completion of folder validation isn't checked for, in the meantime, easier to just hold up the program

            IsValidatingFolders = true;

            foreach (FolderModel imageFolder in imageFolders)
            {
                imageFolder.ValidateImages(updateEnabledState);
            }

            IsValidatingFolders = false;

            //! this is also called by UpdateImageTypeWeights(), keeping this here regardless however to avoid complications in a future refactor since it is critical
            ThemeUtil.ThemeSettings.FrequencyCalc.VerifyImageTypeExistence();
            //! this is also called by UpdateImageTypeWeights(), keeping this here regardless however to avoid complications in a future refactor since it is critical

            ThemeUtil.Theme.RankController.UpdateImageTypeWeights(); //? this is disabled during the loading process and needs to be called once loading is finished to update frequencies & weights

            TaggingUtil.HighlightTags();
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
