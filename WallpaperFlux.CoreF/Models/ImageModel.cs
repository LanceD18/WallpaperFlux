using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using LanceTools.DiagnosticsUtil;
using MvvmCross.Commands;
using WallpaperFlux.Core.Util;

namespace WallpaperFlux.Core.Models
{
    //TODO You should verify if the extension is valid (Look into the methods you used for this in WallpaperManager to determine said extensions)
    public class ImageModel
    {
        public string Path { get; set; }

        public int Rank { get; set; }

        public IMvxCommand ViewFileCommand { get; set; }

        public IMvxCommand OpenFileCommand { get; set; }

        public ImageModel()
        {
            ViewFileCommand = new MvxCommand(ViewFile);
            OpenFileCommand = new MvxCommand(OpenFile);
        }

        // opens the file's folder in the explorer and selects it to navigate the scrollbar to the file
        public void ViewFile()
        {
            if (!ValidationUtil.FileExists(Path)) return;
            ProcessUtil.SelectFile(Path);
        }

        // opens the file
        public void OpenFile()
        {
            if (!ValidationUtil.FileExists(Path)) return;
            ProcessUtil.OpenFile(Path);
        }
    }
}
