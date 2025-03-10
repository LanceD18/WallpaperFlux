﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.VisualBasic.FileIO;
using WallpaperFlux.Core.IoC;
using WallpaperFlux.Core.Util;

namespace WallpaperFlux.WPF.IoC
{
    public class ExternalFileSystemUtil : IExternalFileSystemUtil
    {
        public void RecycleFile(string imagePath)
        {
            try
            {
                if (WallpaperUtil.IsSupportedFileType(imagePath)) // ? to not accidentally delete something random
                {
                    FileSystem.DeleteFile(imagePath, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e); // the file was likely in use, followed by the operation being cancelled, which leads to this exception
            }
        }
    }
}
