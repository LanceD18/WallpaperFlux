using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using LanceTools.DiagnosticsUtil;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using WallpaperFlux.Core.Util;

namespace WallpaperFlux.Core.Models
{
    // the FolderModel will pick up every file within it and attempt to enable/disable it based on the model's active state & whether or not the file is within the theme
    public class FolderModel : MvxNotifyPropertyChanged
    {
        private string _path;

        public string Path
        {
            get => _path;

            private set => _path = value;
        }

        // TODO Rename this to Enabled to match with similar functions across other aspects of the program
        private bool _active;

        public bool Active
        {
            get => _active;

            set
            {
                _active = value;
                RaisePropertyChanged(() => Active); // TODO You should probably change this to SetProperty()
                ValidateImages();
            }
        }

        public string PriorityName { get; set; }

        private List<string> _images
        {
            get
            {
                if (Directory.Exists(Path))
                {
                    return new DirectoryInfo(Path).GetFiles().Select((s) => s.FullName).ToList();
                }

                return new List<string>();
            }
        }

        public FolderModel(string path, bool active, string priorityName = "")
        {
            if (!Directory.Exists(path))
            {
                path = "INVALID PATH ERROR: " + path;
                Path = path;
                return; //! doing anything else could cause errors
            }

            //? internally adds the images to the model (see setter)
            Path = path;

            //! this will internally validate the image folder so this must be placed after the images are added
            Active = active;

            PriorityName = priorityName;

            ViewFolderCommand = new MvxCommand(ViewFolder);
        }

        public IMvxCommand ViewFolderCommand { get; set; }

        public void ViewFolder()
        {
            if (!ValidationUtil.DirectoryExists(Path)) return;
            ProcessUtil.OpenFile(Path);
        }

        // updated the active state of each image based on the folder's active state
        // if the image does not exist, add it to the theme, regardless of whether or not the folder is active
        //? This serves a dual purpose, enabling/disabling images within a folder AND detecting new images upon validation
        // TODO Optimize Me
        public void ValidateImages()
        {
            //! This currently doesn't check for non-image files

            foreach (string image in _images)
            {
                if (ThemeUtil.Theme.Images.ContainsImage(image))
                {
                    ThemeUtil.Theme.Images.GetImage(image).Active = Active;
                }
                else
                {
                    if (File.Exists(image))
                    {
                        ThemeUtil.Theme.Images.AddImage(image);
                        ThemeUtil.Theme.Images.GetImage(image).Active = Active;
                    }
                    else //? this image was removed, delete it
                    {
                        _images.Remove(image);
                    }
                }
            }
        }

        public void RemoveAllImagesOfFolder()
        {
            foreach (string image in _images)
            {
                ThemeUtil.Theme.Images.RemoveImage(image);
            }
        }

        public string[] GetImagePaths()
        {
            ValidateImages(); // check for potentially deleted images

            return _images.ToArray();
        }

        public ImageModel[] GetImageModels() => ThemeUtil.Theme.Images.GetImageRange(GetImagePaths());
    }
}
