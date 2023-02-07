using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LanceTools.IO;
using LanceTools.WPF.Adonis.Util;

namespace WallpaperFlux.Core.Util
{
    public static class ValidationUtil
    {
        public static bool FileExists(string path)
        {
            if (!FileUtil.Exists(path))
            {
                MessageBoxUtil.ShowError("This file does not exist");
                return false;
            }

            return true;
        }

        public static bool DirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                MessageBoxUtil.ShowError("This directory does not exist");
                return false;
            }

            return true;
        }

        public static bool PathExists(string path)
        {
            if (FileUtil.Exists(path) || Directory.Exists(path))
            {
                return true;
            }
            else
            {
                MessageBoxUtil.ShowError("The given path does not exist");
                return false;
            }
        }
    }
}
