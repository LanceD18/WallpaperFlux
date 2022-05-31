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

            private set
            {
                _path = value;
                _images = new DirectoryInfo(Path).GetFiles().Select((s) => s.FullName);
            }
        }

        private bool _active;

        public bool Active
        {
            get => _active;

            set
            {
                _active = value;
                RaisePropertyChanged(() => Active);
                ValidateImages();
            }
        }

        private IEnumerable<string> _images;

        public FolderModel(string path, bool active)
        {
            if (!Directory.Exists(path)) throw new DirectoryNotFoundException(); // ! Handle this outside of this constructor!

            //? internally adds the images to the model (see setter)
            Path = path;

            //! this will internally validate the image folder so this must be placed after the images are added
            Active = active;

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
            foreach (string image in _images)
            {
                if (DataUtil.Theme.Images.ContainsImage(image))
                {
                    DataUtil.Theme.Images.GetImage(image).Active = Active;
                }
                else
                {
                    DataUtil.Theme.Images.AddImage(image);
                    DataUtil.Theme.Images.GetImage(image).Active = Active;
                }
            }
        }

        public void DeactivateFolder()
        {
            foreach (string image in _images)
            {
                DataUtil.Theme.Images.RemoveImage(image);
            }
        }
    }
}
