using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AdonisUI.Controls;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using WallpaperFlux.Core.Models.Controls;
using WallpaperFlux.Core.Util;
using WallpaperFlux.Core.ViewModels;

namespace WallpaperFlux.Core.Models.Tagging
{
    public class FolderPriorityModel : ListBoxItemModel
    {
        private string _conflictResolutionFolder = string.Empty;
        public string Name { get; private set; }

        public string ConflictResolutionFolder
        {
            get => _conflictResolutionFolder;
            private set
            {
                SetProperty(ref _conflictResolutionFolder, value);
                RaisePropertyChanged(() => ConflictResolutionFolderContextMenuString);
            }
        }

        public string ConflictResolutionFolderContextMenuString
        {
            get
            {
                if (ConflictResolutionFolder != string.Empty)
                {
                    return "Conflict Resolution Folder [" + new FileInfo(ConflictResolutionFolder).Name + "]";
                }

                return "No Conflict Resolution Folder Assigned";

            }
        }

        public int AssignedFolderCount => WallpaperFluxViewModel.Instance.ImageFolders.Count(f => f.PriorityIndex == PriorityIndex);

        public int PriorityIndex => TagViewModel.Instance.FolderPriorities.IndexOf(this);

        public IMvxCommand RenameCommand { get; set; }

        public IMvxCommand AssignFolderCommand { get; set; }

        public IMvxCommand RemoveFolderCommand { get; set; }

        public IMvxCommand ListAssignedFoldersCommand { get; set; }

        public IMvxCommand AssignConflictResolutionFolder { get; set; }

        public IMvxCommand RemoveConflictResolutionFolder { get; set; }

        public FolderPriorityModel(string name, string conflictResolutionFolder = "")
        {
            Name = name;
            ConflictResolutionFolder = conflictResolutionFolder;
            RenameCommand = new MvxCommand(Rename);
            AssignFolderCommand = new MvxCommand(AssignFolder);
            RemoveFolderCommand = new MvxCommand(RemoveFolder);
            ListAssignedFoldersCommand = new MvxCommand(ListAssignedFolders);
            AssignConflictResolutionFolder = new MvxCommand(() => ConflictResolutionFolder = FolderUtil.GetValidFolderPath());
            RemoveConflictResolutionFolder = new MvxCommand(() => ConflictResolutionFolder = string.Empty);
        }

        public void Rename()
        {
            Name = MessageBoxUtil.GetString("Folder Priority Name", "Give a name for your priority", "Priority name...");
            RaisePropertyChanged(() => Name);
        }

        /// <summary>
        /// Prompts the user to select a folder and assigns the selected folder to the given priority if the folder is valid
        /// </summary>
        public void AssignFolder()
        {
            FolderModel selectedFolder = FolderUtil.GetValidFolderModel();
            if (selectedFolder != null)
            {
                selectedFolder.PriorityIndex = PriorityIndex;
                RaisePropertyChanged(() => AssignedFolderCount);
            }
        }

        public void RemoveFolder()
        {
            FolderModel selectedFolder = FolderUtil.GetValidFolderModel();
            if (selectedFolder != null)
            {
                selectedFolder.PriorityIndex = -1;
                RaisePropertyChanged(() => AssignedFolderCount);
            }
        }

        /// <summary>
        /// Remove all assigned folders from the given priority
        /// </summary>
        public void ClearFolders()
        {
            foreach (FolderModel folder in WallpaperFluxViewModel.Instance.ImageFolders)
            {
                if (folder.PriorityIndex == PriorityIndex)
                {
                    folder.PriorityIndex = -1;
                    RaisePropertyChanged(() => AssignedFolderCount);
                }
            }
        }

        public void ListAssignedFolders()
        {
            string assignedFolders = "";

            foreach (FolderModel folder in WallpaperFluxViewModel.Instance.ImageFolders)
            {
                if (folder.PriorityIndex == PriorityIndex)
                {
                    assignedFolders += folder.Path + "\n";
                }
            }

            MessageBox.Show(assignedFolders);
        }
    }
}
