using System;
using System.Collections.Generic;
using System.Text;
using MvvmCross.Platforms.Wpf.Views;
using MvvmCross.ViewModels;
using WallpaperFlux.Core.Util;

namespace WallpaperFlux.WPF.Util
{
    public static class WindowUtil
    {
        public const float TAGGING_WINDOW_WIDTH = TaggingUtil.TAGGING_WINDOW_WIDTH;
        public const float TAGGING_WINDOW_HEIGHT = TaggingUtil.TAGGING_WINDOW_HEIGHT;

        public const float SETTINGS_WINDOW_WIDTH = 700;
        public const float SETTINGS_WINDOW_HEIGHT = 615;

        // The windows using this initializer will be closed and re-opened throughout the life of the program
        // This allows the data of the closed window to be preserved through a static "Instance" variable
        public static T InitializeViewModel<T>(T instance)
        {
            if (instance == null)
            {
                return (T)Activator.CreateInstance(typeof(T));
            }
            else
            {
                return instance;
            }
        }
    }
}
