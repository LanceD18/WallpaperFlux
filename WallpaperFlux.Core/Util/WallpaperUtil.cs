using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using LanceTools.WindowsUtil;
using Microsoft.WindowsAPICodePack.Dialogs;
using MvvmCross;
using WallpaperFlux.Core.IoC;
using WallpaperFlux.Core.Models;
using WallpaperFlux.Core.Models.Theme;
using WallpaperFlux.Core.ViewModels;

namespace WallpaperFlux.Core.Util
{
    public static class WallpaperUtil
    {
        //xpublic static Action<int, string> OnWallpaperChange;
        //xpublic static Action<int, WallpaperStyle> OnWallpaperStyleChange;

        public static IExternalWallpaperHandler WallpaperHandler = Mvx.IoCProvider.Resolve<IExternalWallpaperHandler>();
        public static IExternalDisplayUtil DisplayUtil = Mvx.IoCProvider.Resolve<IExternalDisplayUtil>();

        //-----File Types-----
        private static readonly string IMAGE_FILES_DISPLAY_NAME = "Image Files (*.jpg, *.jpeg, *.jpe, *.jfif, *.png, *.gif, *.mp4, *.webm, *.avi)";
        private static readonly string IMAGE_FILES_EXTENSION_LIST = "*.jpg; *.jpeg; *.jpe; *.jfif; *.png; *.gif; *.mp4; *.webm; *.avi";

        private static readonly string ALL_FILES_DISPLAY_NAME = "All Files (*.*)";
        private static readonly string ALL_FILES_EXTENSION_LIST = ".*";

        public static void AddImageFilesFilterToDialog(this CommonOpenFileDialog dialog)
        {
            dialog.Filters.Add(new CommonFileDialogFilter(IMAGE_FILES_DISPLAY_NAME, IMAGE_FILES_EXTENSION_LIST));
            dialog.Filters.Add(new CommonFileDialogFilter(ALL_FILES_DISPLAY_NAME, ALL_FILES_EXTENSION_LIST));
        }

        public static bool IsStatic(string filePath) => IsStatic_GivenExtension(Path.GetExtension(filePath));

        private static bool IsStatic_GivenExtension(string extension) => !(extension == ".gif" || WallpaperUtil.IsSupportedVideoType_GivenExtension(extension));

        public static bool IsGif(string filePath) => IsGif_GivenExtension(Path.GetExtension(filePath));

        public static bool IsGif_GivenExtension(string extension) => extension == ".gif";

        public static bool IsVideo(string filePath) => IsVideo_GivenExtension(Path.GetExtension(filePath));

        public static bool IsVideo_GivenExtension(string extension) => IsSupportedVideoType_GivenExtension(extension);

        public static bool IsSupportedVideoType(string filePath)
        {
            return IsSupportedVideoType_GivenExtension(Path.GetExtension(filePath));

            /*x
            if (File.Exists(filePath))
            {
                return IsSupportedVideoType_GivenExtension(filePath);
            }
            else
            {
                return false;
            }
            */
        }

        private static bool IsSupportedVideoType_GivenExtension(string extension) => extension == ".mp4" || extension == ".webm" || extension == ".avi";

        // Derived from: https://www.codeproject.com/Articles/856020/Draw-Behind-Desktop-Icons-in-Windows-plus
        // Gets the IntPtr value that will allow us to draw the wallpaper behind the desktop icons
        public static IntPtr GetDesktopWorkerW()
        {
            //?-----Fetch the Program window-----
            //! Progman (not program) allows the form to be represented as a child window of the desktop itself
            IntPtr progman = Win32.FindWindow("Progman", null);

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

            // We enumerate all Windows, until we find one, that has the SHELLDLL_DefView as a child. 
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

        public static void SetWallpaper(int index, bool ignoreRandomization)
        {
            SetWallpaper(index, String.Empty, ignoreRandomization);
        }

        // TODO With the presetWallpaperPath I don't think you need ignoreRandomization anymore
        // TODO Both use cases, setting the PreviousWallpaper and directly setting an image as the Wallpaper can use presetWallpaperPath
        public static bool SetWallpaper(int index, string presetWallpaperPath, bool ignoreRandomization)
        {
            string wallpaperPath;

            // Set Next Wallpaper
            if (presetWallpaperPath == String.Empty)
            {
                // if ignoring randomization then we will just use the current ActiveWallpaper path (Likely means that a wallpaper on one monitor changed before/after the others)
                if (!ignoreRandomization)
                {
                    if (DataUtil.Theme.WallpaperRandomizer.SetNextWallpaperOrder(index))
                    {
                        wallpaperPath = DataUtil.Theme.WallpaperRandomizer.ActiveWallpapers[index];
                    }
                    else
                    {
                        Debug.WriteLine("Failed to set wallpaper, improper randomization");
                        return false;
                    }
                }
                else
                {
                    wallpaperPath = DataUtil.Theme.WallpaperRandomizer.ActiveWallpapers[index];
                }
            }
            else
            {
                wallpaperPath = presetWallpaperPath;
                DataUtil.Theme.WallpaperRandomizer.ActiveWallpapers[index] = presetWallpaperPath; // need to update the active wallpaper to reflect this preset change
            }

            // Check for errors and update the notify icon
            if (DataUtil.Theme.Images.ContainsImage(wallpaperPath))
            {
                /* TODO Update notify icon
                string wallpaperName = new FileInfo(wallpaperPath).Name; // pathless string of file name
                wallpaperMenuItems[index].Text = index + 1 + " | R: " + DataUtil.Theme.Images.GetImage(wallpaperPath).Rank + " | " + wallpaperName;
                */
            }
            else
            {
                // TODO Ensure that the below is no longer an issue of the redesign, if a failure occurs we will just not load the next wallpaper instead
                //? this can occur after swapping themes as next wallpaper still holds data from the previous theme

                Debug.WriteLine("Failed to set wallpaper, no images exist");
                return false;
                //xDataUtil.Theme.WallpaperRandomizer.SetNextWallpaperOrder(index, ignoreRandomization);
            }

            Debug.WriteLine("Settings Wallpaper to Display " + index + ": " + wallpaperPath);
            // TODO Want want to use IoC for this at some point
            if (DataUtil.Theme.Images.ContainsImage(wallpaperPath))
            {
                WallpaperHandler.OnWallpaperChange(index, DataUtil.Theme.Images.GetImage(wallpaperPath)); //? hooked to a call from WallpaperFlux.WPF
                WallpaperFluxViewModel.Instance.DisplaySettings[index].ResetTimer(true);
            }
            return true;
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