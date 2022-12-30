using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Text;
using AdonisUI.Controls;
using LanceTools;
using LanceTools.IO;
using Microsoft.WindowsAPICodePack.Dialogs;
using WallpaperFlux.Core.Models;
using WallpaperFlux.Core.Util;
using WallpaperFlux.Core.ViewModels;

namespace WallpaperFlux.Core.Tools
{
    public static class ImageRenamer
    {
        // TODO Test if the new local variable works before removing this
        //xprivate static readonly string TempImageLocation = Path.GetDirectoryName(MediaTypeNames.Application.ExecutablePath) + @"\WallpaperData\TempImageLocation.file";

        // TODO Consider re-adding this at some point, not really something you'd use at the moment but it's an option
        /*x
        private static ImageType[] GetFilter()
        {
            List<ImageType> filter = new List<ImageType>();

            if (OptionsData.ThemeOptions.ExcludeRenamingStatic) filter.Add(ImageType.Static);
            if (OptionsData.ThemeOptions.ExcludeRenamingGif) filter.Add(ImageType.GIF);
            if (OptionsData.ThemeOptions.ExcludeRenamingVideo) filter.Add(ImageType.Video);

            return filter.ToArray();
        }

        private static ImageModel[] FilterImages(ImageModel[] images)
        {
            ImageType[] filter = GetFilter();

            if (filter.Length == 0) return images; // nothing will be filtered out

            List<ImageModel> imagesToRename = new List<ImageModel>();
            foreach (ImageModel image in images)
            {
                if (filter.Contains(image.ImageType)) continue; // filters out this image

                imagesToRename.Add(image);
            }

            return imagesToRename.ToArray();
        }
        */

        #region Tag-Based Auto Naming

        public static void AutoRenameImage(ImageModel image)
        {
            RenameWithTagBasedNaming(new ImageModel[] { image });
        }

        public static void AutoRenameImageRange(ImageModel[] images)
        {
            RenameWithTagBasedNaming(images);
        }

        public static void AutoMoveImage(ImageModel image)
        {
            string folderPath = FolderUtil.GetValidFolderPath();

            if (folderPath != string.Empty)
            {
                RenameWithTagBasedNaming(new ImageModel[] { image }, new DirectoryInfo(folderPath));
            }
        }

        public static void AutoMoveImageRange(ImageModel[] images)
        {
            string folderPath = FolderUtil.GetValidFolderPath();

            if (folderPath != string.Empty)
            {
                RenameWithTagBasedNaming(images, new DirectoryInfo(folderPath));
            }
        }

        private static void RenameWithTagBasedNaming(ImageModel[] images, DirectoryInfo moveDirectory = null)
        {
            if (images.Length > 1)
            {
                if (!MessageBoxUtil.PromptYesNo("Are you sure you want to rename ALL " + images.Length + " images?")) return;
            }

            string renamingErrors = "Errors were found while attempting to rename the following image(s): \n\n";
            bool renamingErrorFound = false;

            Dictionary<string, Dictionary<string, HashSet<ImageModel>>> desiredNames = GetDesiredNames(images, moveDirectory);

            //! Since we are dealing with active files here and not objects:
            //! targeted images should not be touched until they are ready to be renamed (Using File.Move())

            //xbool groupRenamedImages = images.Length > 1 && MessageBoxUtil.PromptYesNo("Would you like to group together images with the same tag combination on renaming?");
            //xDebug.WriteLine("Grouping: " + groupRenamedImages);
            
            foreach (string directory in desiredNames.Keys)
            {
                Debug.WriteLine("\n\nDirectory: " + directory);

                // generates a string HashSet of all files within this directory
                //? remember to use .ToLower() since we are ignoring case sensitivity for the naming here
                // TODO Would ignoring case sensitivity really matter in a theme that starts from this version? The only reason it's been kept on it for "legacy compatibility"
                HashSet<string> filePaths = new DirectoryInfo(directory).GetFiles().Select(f =>
                    f.FullName.Substring(0, f.FullName.IndexOf(f.Extension, StringComparison.Ordinal)).ToLower()).ToHashSet();

                foreach (string name in desiredNames[directory].Keys)
                {
                    int nameCount = 1; // image counts don't start at 0
                    bool canName = false;
                    string directoryPath = directory + "\\";
                    string nameWithoutCount = directoryPath + name;
                    string nameToStartWith = nameWithoutCount + nameCount;

                    //? ----- Adjust nameCount to a usable position -----
                    // if groupRenamedImages is true, this process will repeat until a group-able section is found, otherwise we'll only loop through this once
                    while (!canName)
                    {
                        // finds the next possible name to start with
                        // will check files 1 by 1 until an opening is found, then check if a group opening is valid if grouping is enabled
                        while (filePaths.Contains(nameToStartWith.ToLower()))
                        {
                            nameCount++;
                            nameToStartWith = nameWithoutCount + nameCount;
                        }

                        Debug.WriteLine("Checkpoint Starting Name: " + nameToStartWith);

                        canName = true;
                        // no need to group if there's only 1 image
                        // Checks for a spacing where the given group can fit to ensure that a group of images can be renamed together
                        if (WallpaperFluxViewModel.Instance.GroupRenamed && images.Length > 1)
                        {
                            int groupSize = desiredNames[directory][name].Count;
                            for (int i = 0; i < groupSize; i++)
                            {
                                string testName = nameWithoutCount + (nameCount + i);
                                
                                if (filePaths.Contains(testName.ToLower())) //! Note: nameCount should only change if the process fails to find a space for the group
                                {
                                    Debug.WriteLine("Grouping Failed At: " + testName);
                                    nameCount += i + 1; // sets the count to the next possibly valid position (i.e, skipping over the area traversed by the group)
                                    canName = false;
                                    break;
                                }
                            }
                        }
                    }

                    Debug.WriteLine("Final Name to Start With: " + nameToStartWith);

                    //? ----- Apply nameCount to image group -----
                    foreach (ImageModel image in desiredNames[directory][name])
                    {
                        string oldPath = image.Path;
                        string newPathWithoutExtension = nameWithoutCount + nameCount;

                        // last call for conflicts
                        //? this is expected to occur if the images aren't grouped, if the images are grouped then we have an error
                        //?  (processing time for grouped selections shouldn't be large)
                        while (filePaths.Contains(newPathWithoutExtension.ToLower()))
                        {
                            nameCount++;
                            newPathWithoutExtension = nameWithoutCount + nameCount;
                        }

                        // --------! Conflicts Resolved! Rename the next available image !--------

                        string extension = new FileInfo(image.Path).Extension;
                        string newPath = newPathWithoutExtension + extension;
                        Debug.WriteLine("Old Path: " + oldPath + "\nNew Path: " + newPath);
                        nameCount++;

                        if (!ValidateImagePathUpdate(oldPath, newPath, image, out string errorMessage))
                        {
                            //? error message will exist if we get here, so just add it
                            renamingErrors += errorMessage;
                            renamingErrorFound = true;
                        }
                    }
                }
            }

            WallpaperFluxViewModel.Instance.RaisePropertyChanged(() => WallpaperFluxViewModel.Instance.SelectedImagePathText);

            if (renamingErrorFound) MessageBoxUtil.ShowError(renamingErrors);

            //! It's important to save after renaming as if the user forgets all of the renamed images will lose their ranking when the user next loads back into the theme
            JsonUtil.QuickSave();
        }

        private static Dictionary<string, Dictionary<string, HashSet<ImageModel>>> GetDesiredNames(ImageModel[] images, DirectoryInfo moveDirectory)
        {
            string failedToNameException = "No name could be created for the following images." +
                                           "\nThese images either have no tags or all of their tags cannot be used for naming:\n";
            bool failedToNameAnImage = false;

            Dictionary<string, Dictionary<string, HashSet<ImageModel>>> desiredNames = new Dictionary<string, Dictionary<string, HashSet<ImageModel>>>();

            foreach (ImageModel image in images)
            {
                // ----- Get & Validate Desired Name -----
                string desiredName = image.GetTaggedName();

                if (desiredName == "")
                {
                    failedToNameException += "\n" + image.Path;
                    failedToNameAnImage = true;
                    continue;
                }

                // ----- Attach Image to Respective Desired Name Collection -----
                // if a moveDirectory is present use that, otherwise, use the image's directory
                string directory = moveDirectory == null ? image.PathFolder : moveDirectory.FullName;
                Debug.WriteLine("Directory: " + directory + " | DesiredName: " + desiredName + " | Image: " + image.PathFolder);

                if (!desiredNames.ContainsKey(directory)) // add directories that we have not encountered yet
                {
                    Debug.WriteLine("New Directory");
                    desiredNames.Add(directory, new Dictionary<string, HashSet<ImageModel>>());
                }

                if (desiredNames[directory].ContainsKey(desiredName)) // if this desired name has been encountered, just add the image to the collection corresponding to this desired name
                {
                    desiredNames[directory][desiredName].Add(image);
                }
                else // if this desired name has not been encountered yet, start up a new image collection with this name
                {
                    Debug.WriteLine("New Name");
                    desiredNames[directory].Add(desiredName, new HashSet<ImageModel> { image });
                }
            }

            // TODO THIS SHOULD LATER EXCLUDE IMAGES THAT CANNOT BE RENAMED
            if (failedToNameAnImage)
            {
                MessageBox.Show(failedToNameException);
            }

            return desiredNames;
        }

        #endregion

        #region Direct Naming
        public static void DirectlyRenameImage(ImageModel image)
        {
            RenameWithDirectNaming(new ImageModel[] { image });
        }

        public static void DirectlyRenameImageRange(ImageModel[] images)
        {
            RenameWithDirectNaming(images);
        }

        public static void DirectlyMoveImage(ImageModel image)
        {
            string folderPath = FolderUtil.GetValidFolderPath();

            if (folderPath != string.Empty)
            {
                RenameWithDirectNaming(new ImageModel[] { image }, new DirectoryInfo(folderPath));
            }
        }

        public static void DirectlyMoveImageRange(ImageModel[] images)
        {
            string folderPath = FolderUtil.GetValidFolderPath();

            if (folderPath != string.Empty)
            {
                RenameWithDirectNaming(images, new DirectoryInfo(folderPath));
            }
        }

        private static void RenameWithDirectNaming(ImageModel[] images, DirectoryInfo moveDirectory = null)
        {
            // TODO Renaming an image gives it a direct name that you input yourself
            // TODO Renaming multiple images gives all images that name but using the numbering system
            // TODO Moving an image does not change its name, if an image with the name already exists perform typical number incrementation (but notify the user in this case)

            if (images.Length > 1)
            {
                if (!MessageBoxUtil.PromptYesNo("Are you sure you want to rename ALL " + images.Length + " images?")) return;
            }

            //! It's important to save after renaming as if the user forgets all of the renamed images will lose their ranking when the user next loads back into the theme
            JsonUtil.QuickSave();
        }
        #endregion

        #region Image Path Updating
        private static bool ValidateImagePathUpdate(string oldPath, string newPath, ImageModel image, out string errorMessage)
        {
            // TODO THESE ERROR MESSAGES SHOULD BE COMPILED INTO ONE LARGE MESSAGE, REMEMBER THAT YOU HAVE A SCROLLBAR NOW
            if (image != null && File.Exists(oldPath))
            {
                if (!File.Exists(newPath))
                {
                    if (new FileInfo(newPath).Name.IndexOfAny(Path.GetInvalidFileNameChars()) == -1)
                    {
                        return TryToUpdateImagePath(oldPath, newPath, out errorMessage);
                    }
                    else
                    {
                        errorMessage = "This image could not be renamed as its name contains invalid characters: \n" + oldPath;
                    }
                }
                else
                {
                    errorMessage = "This image could not be renamed as its intended path has already been taken: " +
                                   "\nOld Path: " + oldPath +
                                   "\nNew Path: " + newPath;
                }
            }
            else
            {
                errorMessage = "Image not found, cannot rename: \n" + oldPath;
            }

            return false;
        }

        private static bool TryToUpdateImagePath(string oldPath, string newPath, out string errorMessage)
        {
            try
            {
                //? Since File.Move is case insensitive, first we need to check if oldPath and newPath has the same letters when cases are ignored
                if (string.Equals(oldPath, newPath, StringComparison.CurrentCultureIgnoreCase))
                {
                    //? If oldPath and newPath have the same letters, move the file to a temporary location then move it back to the intended location

                    string tempImageLocation = oldPath + ".temp"; //! If something is broken here refer to the old version of this under: TempImageLocation, will require some IoC control fixes
                    File.Move(oldPath, tempImageLocation);
                    File.Move(tempImageLocation, newPath);
                }
                else // otherwise, if the cases do not matter, move the file normally
                {
                    File.Move(oldPath, newPath);
                }

                ThemeUtil.Theme.Images.GetImage(oldPath).UpdatePath(newPath); //? we rename the image object after moving for just in case an error prevents the move from happening

                errorMessage = ""; // no errors found

                return true;
            }
            catch (Exception e)
            {
                // Most likely cause of an error is that the file was being used by another process
                List<Process> processes = FileUtil.WhoIsLocking(oldPath);
                if (processes.Count > 0)
                {
                    string processOutput = oldPath + "\nThe above image is being used by the following process: ";

                    for (int i = 0; i < processes.Count; i++)
                    {
                        processOutput += "\n" + processes[i].ProcessName;
                    }

                    errorMessage = processOutput; //! error output
                }
                else
                {
                    errorMessage = "Error encountered, image failed to change: \n[" + oldPath + "] " +
                                   "\n\nIntended new path: \n[" + newPath + "] " +
                                   "\n\nError: " + e.Message; //! error output
                }

                return false;
            }
        }
        #endregion
    }
}
