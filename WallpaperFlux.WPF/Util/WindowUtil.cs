using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Windows;
using MvvmCross.Platforms.Wpf.Views;
using MvvmCross.ViewModels;
using WallpaperFlux.Core.Util;
using WallpaperFlux.Core.ViewModels;
using WallpaperFlux.WPF.Views;

namespace WallpaperFlux.WPF.Util
{
    public static class WindowUtil
    {
        // TODO Consider consolidating all View Presenters into WindowUtil
        //? Oddities like ImageSelectionPresenter are forced to be initialized here instead of WallpaperFluxView.xaml.cs due to not having a reference that window
        public static ViewPresenter ImageSelectionPresenter;

        public const float TAGGING_WINDOW_WIDTH = TaggingUtil.TAGGING_WINDOW_WIDTH;
        public const float TAGGING_WINDOW_HEIGHT = TaggingUtil.TAGGING_WINDOW_HEIGHT;

        public const float SETTINGS_WINDOW_WIDTH = 700;
        public const float SETTINGS_WINDOW_HEIGHT = 615;

        private const float IMAGE_SELECTION_WINDOW_WIDTH = 410;
        private const float IMAGE_SELECTION_WINDOW_HEIGHT = 265;

        public static void InitializeViewModels()
        {
            TagViewModel.Instance = InitializeViewModel(TagViewModel.Instance);
            SettingsViewModel.Instance = InitializeViewModel(SettingsViewModel.Instance);
            ImageSelectionViewModel.Instance = InitializeViewModel(ImageSelectionViewModel.Instance);
        }

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

        public static void PresentImageSelectionView() =>
            PresentWindow(ref ImageSelectionPresenter, typeof(ImageSelectionView), typeof(ImageSelectionViewModel),
                IMAGE_SELECTION_WINDOW_WIDTH, IMAGE_SELECTION_WINDOW_HEIGHT, "Image Selection Options", false);

        public static void PresentWindow(ref ViewPresenter presenter, Type viewType, Type viewModelType, float width, float height, string title, bool modal)
        {
            if (presenter == null || presenter.ViewWindow == null) // for the case where either the presenter or the view itself do not exist
            {
                presenter = new ViewPresenter(viewType, viewModelType, width, height, title, modal);
            }
            else // if the window is already open, just focus it
            {
                if (presenter.ViewWindow.WindowState == WindowState.Minimized)
                {
                    presenter.ViewWindow.WindowState = WindowState.Normal;
                }

                presenter.ViewWindow.Focus();
            }
        }

        public static void CloseWindow(ViewPresenter presenter) => presenter.ViewWindow.Close();
    }
}
