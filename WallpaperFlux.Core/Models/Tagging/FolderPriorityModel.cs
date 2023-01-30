using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using AdonisUI.Controls;
using LanceTools.WPF.Adonis.Util;
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

        private string _name;

        public string Name
        {
            get => _name;
            private set
            {
                UpdateFolders(_name, value);
                SetProperty(ref _name, value);
            }
        }

        public string ConflictResolutionFolder
        {
            get => _conflictResolutionFolder;
            private set
            {
                SetProperty(ref _conflictResolutionFolder, value);
                RaisePropertyChanged(() => ConflictResolutionFolderContextMenuText);
            }
        }

        public string ConflictResolutionFolderContextMenuText
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

        public int AssignedFolderCount => WallpaperFluxViewModel.Instance.ImageFolders.Count(f => f.PriorityName == Name);

        //? Instead of using the index of the priority, this priority will mimic the behavior of another priority,
        //? using their conflict resolution in cases that go beyond the scope of this priority's tags
        private int _priorityOverride = -1;
        public int PriorityOverride
        {
            get => _priorityOverride;
            set
            {
                SetProperty(ref _priorityOverride, value);
                RaisePropertyChanged(() => PriorityOverrideText);
            }
        }

        public string PriorityOverrideText
        {
            get
            {
                if (PriorityOverride != -1)
                {
                    return "Priority Override: " + PriorityOverride;
                }

                return "Using Default Priority";
            }
        }

        #region Commands
        public IMvxCommand RenameCommand { get; set; }

        public IMvxCommand AssignFolderCommand { get; set; }

        public IMvxCommand RemoveFolderCommand { get; set; }

        public IMvxCommand ListAssignedFoldersCommand { get; set; }

        public IMvxCommand AssignConflictResolutionFolderCommand { get; set; }

        public IMvxCommand RemoveConflictResolutionFolderCommand { get; set; }

        public IMvxCommand OverridePriorityCommand { get; set; }
        #endregion

        public FolderPriorityModel(string name, string conflictResolutionFolder = "", int priorityOverride = -1)
        {
            Name = name;
            ConflictResolutionFolder = conflictResolutionFolder;
            PriorityOverride = priorityOverride;

            RenameCommand = new MvxCommand(Rename);
            AssignFolderCommand = new MvxCommand(AssignFolder);
            RemoveFolderCommand = new MvxCommand(RemoveFolder);
            ListAssignedFoldersCommand = new MvxCommand(ListAssignedFolders);
            AssignConflictResolutionFolderCommand = new MvxCommand(AssignConflictResolutionFolder);
            RemoveConflictResolutionFolderCommand = new MvxCommand(() => ConflictResolutionFolder = string.Empty);
            OverridePriorityCommand = new MvxCommand(SetPriorityOverride);
        }

        public void Rename()
        {
            string priorityName = MessageBoxUtil.GetString("Folder Priority Name", "Give a name for your priority", "Priority name...");

            if (TagViewModel.Instance.ContainsFolderPriority(priorityName))
            {
                MessageBoxUtil.ShowError("The priority [" + priorityName + "] already exists");
                return;
            }

            Name = priorityName;
        }

        /// <summary>
        /// Prompts the user to select a folder and assigns the selected folder to the given priority if the folder is valid
        /// </summary>
        public void AssignFolder()
        {
            FolderModel[] selectedFolders = FolderUtil.GetValidFolderModels();

            foreach (FolderModel selectedFolder in selectedFolders)
            {
                if (selectedFolder != null)
                {
                    selectedFolder.PriorityName = Name;
                }
            }

            RaisePropertyChanged(() => AssignedFolderCount);
        }

        public void RemoveFolder()
        {
            FolderModel[] selectedFolders = FolderUtil.GetValidFolderModels();

            foreach (FolderModel selectedFolder in selectedFolders)
            {
                if (selectedFolder != null)
                {
                    selectedFolder.PriorityName = "";
                }
            }

            RaisePropertyChanged(() => AssignedFolderCount);
        }

        /// <summary>
        /// Remove all assigned folders from the given priority
        /// </summary>
        public void ClearFolders()
        {
            foreach (FolderModel folder in WallpaperFluxViewModel.Instance.ImageFolders)
            {
                if (folder.PriorityName == Name)
                {
                    folder.PriorityName = "";
                }
            }

            RaisePropertyChanged(() => AssignedFolderCount);
        }

        /// <summary>
        /// update assigned folders to the new name
        /// </summary>
        public void UpdateFolders(string oldName, string newName)
        {
            foreach (FolderModel folder in WallpaperFluxViewModel.Instance.ImageFolders)
            {
                if (folder.PriorityName == oldName)
                {
                    folder.PriorityName = newName;
                }
            }
        }

        public void ListAssignedFolders()
        {
            string assignedFolders = "";

            foreach (FolderModel folder in WallpaperFluxViewModel.Instance.ImageFolders)
            {
                if (folder.PriorityName == Name)
                {
                    if (Directory.Exists(folder.Path))
                    {
                        assignedFolders += "[" + new DirectoryInfo(folder.Path).Name + "]\n";
                    }
                }
            }

            MessageBox.Show(assignedFolders);
        }

        public void AssignConflictResolutionFolder()
        {
            string path = FolderUtil.GetValidFolderPath();
            if (!string.IsNullOrEmpty(path))
            {
                ConflictResolutionFolder = path;
            }
        }

        public void SetPriorityOverride()
        {
            if (MessageBoxUtil.GetInteger("Set Priority Override",
                "Enter a value. Choose a value of -1 to turn off the priority override",
                out int priorityOverride, "Priority Override...."))
            {
                PriorityOverride = priorityOverride;
            }
        }
    }
}
