using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using LanceTools.WindowsUtil;
using Microsoft.WindowsAPICodePack.Dialogs;
using WallpaperFlux.Core.Models;
using WallpaperFlux.Core.Models.Theme;
using WallpaperFlux.Core.ViewModels;

namespace WallpaperFlux.Core.Util
{

    public static class WallpaperUtil
    {
        /*
        //! temp
        public static List<ImageModel> Images = new List<ImageModel>();
        //! temp
        */

        public static ThemeModel Theme;

        // WallpaperWindow Events
        public static Action<int, string> OnWallpaperChange;
        public static Action<int, WallpaperStyle> OnWallpaperStyleChange;

        public static string GetWallpaperPath()
        {
            if (Images.Count <= 0) return string.Empty;

            Random rand = new Random();
            int imageIndex = rand.Next(Images.Count);

            return Images[imageIndex].Path;
        }

        public static int DisplayCount { get; private set; }

        public static void SetDisplayCount(int displayCount) => DisplayCount = displayCount;

        //-----File Types-----
        private static readonly string IMAGE_FILES_DISPLAY_NAME = "Image Files (*.jpg, *.jpeg, *.jpe, *.jfif, *.png, *.gif, *.mp4, *.webm, *.avi";
        private static readonly string IMAGE_FILES_EXTENSION_LIST = "*.jpg; *.jpeg; *.jpe; *.jfif; *.png; *.gif; *.mp4; *.webm; *.avi";

        private static readonly string ALL_FILES_DISPLAY_NAME = "All Files (*.*)";
        private static readonly string ALL_FILES_EXTENSION_LIST = ".*";

        public static void AddImageFilesFilterToDialog(this CommonOpenFileDialog dialog)
        {
            dialog.Filters.Add(new CommonFileDialogFilter(IMAGE_FILES_DISPLAY_NAME, IMAGE_FILES_EXTENSION_LIST));
            dialog.Filters.Add(new CommonFileDialogFilter(ALL_FILES_DISPLAY_NAME, ALL_FILES_EXTENSION_LIST));
        }

        public static bool IsSupportedVideoType(string filePath)
        {
            if (File.Exists(filePath))
            {
                return IsSupportedVideoType(new FileInfo(filePath));
            }
            else
            {
                return false;
            }
        }

        public static bool IsSupportedVideoType(FileInfo fileInfo)
        {
            string extension = fileInfo.Extension;
            return extension == ".mp4" || extension == ".webm" || extension == ".avi";
        }

        // Derived from: https://www.codeproject.com/Articles/856020/Draw-Behind-Desktop-Icons-in-Windows-plus
        // Gets the IntPtr value that will allow us to draw the wallpaper behind the desktop icons
        public static IntPtr GetDesktopWorkerW()
        {
            //?-----Fetch the Program window-----
            IntPtr progman = Win32.FindWindow("Progman", null); // progman (not program) allows the form to be represented as a child window of the desktop itself

            //?-----Spawn a WorkerW behind the desktop icons (If it is already there, nothing happens)-----
            IntPtr result = IntPtr.Zero;
            // Send 0x052C to Progman. This message directs Progman to spawn a 
            // WorkerW behind the desktop icons. If it is already there, nothing 
            // happens.
            Win32.SendMessageTimeout(progman,
                0x052C,
                new IntPtr(0),
                IntPtr.Zero,
                Win32.SendMessageTimeoutFlags.SMTO_NORMAL,
                1000,
                out result);

            //?-----Find the Window that's underneath the desktop icons-----
            // Spy++ output
            // .....
            // 0x00010190 "" WorkerW
            //   ...
            //   0x000100EE "" SHELLDLL_DefView
            //     0x000100F0 "FolderView" SysListView32
            // 0x00100B8A "" WorkerW       <-- This is the WorkerW instance we are after!
            // 0x000100EC "Program Manager" Progman

            IntPtr workerw = IntPtr.Zero;

            // We enumerate all Windows, until we find one, that has the SHELLDLL_DefView 
            // as a child. 
            // If we found that window, we take its next sibling and assign it to workerw.
            Win32.EnumWindows(new Win32.EnumWindowsProc((tophandle, topparamhandle) =>
            {
                IntPtr p = Win32.FindWindowEx(tophandle,
                    IntPtr.Zero,
                    "SHELLDLL_DefView",
                    String.Empty);

                if (p != IntPtr.Zero)
                {
                    // Gets the WorkerW Window after the current one.
                    workerw = Win32.FindWindowEx(IntPtr.Zero,
                        tophandle,
                        "WorkerW",
                        String.Empty);
                }

                return true;
            }), IntPtr.Zero);

            return workerw;
        }

        public static void SetWallpaper(int index, string path = null)
        {
            OnWallpaperChange?.Invoke(index, path);
        }

        /*x
        //? Note: For this function to work, the form has to be already created. The form.Load event seems to be the right place for it.
        private void InitializeWallpapers()
        {
            IntPtr workerw = GetDesktopWorkerW();

            int monitorCount = DisplayData.Displays.Length;
            wallpapers = new WallpaperForm.WallpaperForm[monitorCount];
            for (int i = 0; i < monitorCount; i++)
            {
                wallpapers[i] = new WallpaperForm.WallpaperForm(DisplayData.Displays[i], workerw);
            }
        }
        */
    }
}