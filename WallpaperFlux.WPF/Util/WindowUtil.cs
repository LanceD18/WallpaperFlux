using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Windows;
using MvvmCross.Platforms.Wpf.Views;
using MvvmCross.ViewModels;
using WallpaperFlux.Core.Util;
using WallpaperFlux.Core.ViewModels;
using WallpaperFlux.WPF.Views;
using static WallpaperFlux.WPF.Util.WindowUtil;

namespace WallpaperFlux.WPF.Util
{
    public static class WindowUtil
    {
        public static ViewPresenter ImageSelectionPresenter;
        public static ViewPresenter TagPresenter;
        public static ViewPresenter SettingsPresenter;
        public static ViewPresenter PaginationTestPresenter;

        public const float TAGGING_WINDOW_WIDTH = TaggingUtil.TAGGING_WINDOW_WIDTH;
        public const float TAGGING_WINDOW_HEIGHT = TaggingUtil.TAGGING_WINDOW_HEIGHT;

        public const float SETTINGS_WINDOW_WIDTH = 700;
        public const float SETTINGS_WINDOW_HEIGHT = 615;

        private const float IMAGE_SELECTION_WINDOW_WIDTH = 410;
        private const float IMAGE_SELECTION_WINDOW_HEIGHT = 265;

        public const float PAGINATION_TEST_WINDOW_WIDTH = 900;
        public const float PAGINATION_TEST_WINDOW_HEIGHT = 800;

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

        public static void PresentTagView(CancelEventHandler e)
        {
            PresentWindow(ref TagPresenter, typeof(TagView), typeof(TagViewModel),
                TAGGING_WINDOW_WIDTH, TAGGING_WINDOW_HEIGHT, "Tag View", false);

            //! Keep in mind that the ViewWindow will be destroyed upon closing the TagView, so yes we need to add the event again
            //? Prevents the TagBoard from causing a crash the next time the tag view is opened if the tag view is closed with the TagBoard open
            TagPresenter.ViewWindow.Closing += e;
        }

        public static void PresentSettingsView(CancelEventHandler e)
        {
            PresentWindow(ref SettingsPresenter, typeof(SettingsView), typeof(SettingsViewModel),
                SETTINGS_WINDOW_WIDTH, SETTINGS_WINDOW_HEIGHT, "Settings", false);

            //! Keep in mind that the ViewWindow will be destroyed upon closing the view, so yes we need to add the event again
            //? Prevents drawers from causing a crash the next time the view is opened
            SettingsPresenter.ViewWindow.Closing += e;
        }

        public static void PresentPaginationTestView() =>
            PresentWindow(ref PaginationTestPresenter, typeof(PaginationTestView), typeof(PaginationTestViewModel),
                PAGINATION_TEST_WINDOW_WIDTH, PAGINATION_TEST_WINDOW_HEIGHT, "Pagination Test", false);

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

        public static void HideAllWindows()
        {
            HideView(TagPresenter);
            HideView(ImageSelectionPresenter);
            HideView(SettingsPresenter);
            HideView(PaginationTestPresenter);
        }

        public static void HideView(ViewPresenter view)
        {
            if (view != null && view.ViewWindow != null)
            {
                view.ViewWindow.Hide();
            }
        }

        public static void ShowAllWindows()
        {
            ShowView(TagPresenter);
            ShowView(ImageSelectionPresenter);
            ShowView(SettingsPresenter);
            ShowView(PaginationTestPresenter);
        }

        public static void ShowView(ViewPresenter view)
        {
            if (view != null && view.ViewWindow != null)
            {
                view.ViewWindow.Show();
            }
        }

        public static void CloseWindow(ViewPresenter presenter) => presenter.ViewWindow.Close();
    }
}
