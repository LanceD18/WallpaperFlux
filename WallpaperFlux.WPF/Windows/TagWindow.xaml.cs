using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MvvmCross.Platforms.Wpf.Presenters;
using MvvmCross.Platforms.Wpf.Presenters.Attributes;
using MvvmCross.Platforms.Wpf.Views;
using MvvmCross.ViewModels;
using WallpaperFlux.Core.ViewModels;
using WallpaperFlux.WPF.Views;
using WallpaperFlux.Core.Util;

namespace WallpaperFlux.WPF.Windows // TODO Consider moving all other windows to this namespace as well (MainWindow, InputBoxView [Yes, this is also a window oops], WallpaperWindow)
{
    //! FYI, the window doesn't actually do much of anything but send over the attribute, which in itself can be done elsewhere
    /// <summary>
    /// Interaction logic for TagWindow.xaml
    /// </summary>
    [MvxWindowPresentation(Identifier = nameof(TagWindow), Modal = false)]
    public partial class TagWindow : MvxWindow
    {
        public ViewPresenter Presenter;

        public TagWindow()
        {
            InitializeComponent();

            MvxWindowPresentationAttribute attribute = new MvxWindowPresentationAttribute
            {
                Identifier = nameof(TagWindow),
                Modal = false
            };

            Presenter = new ViewPresenter();
            Presenter.Show(attribute, typeof(TagView), typeof(TagViewModel), TaggingUtil.TAGGING_WINDOW_WIDTH, TaggingUtil.TAGGING_WINDOW_HEIGHT, "Tag View");

            //xMvxWpfViewPresenter mvxPresenter = new MvxWpfViewPresenter(this);
            //xmvxPresenter.Show(new MvxViewModelInstanceRequest(typeof(TagViewModel)));

            /*x
            MvxWpfViewPresenter presenter = new MvxWpfViewPresenter(new TagView());
            MvxViewModelRequest request = new MvxViewModelInstanceRequest(typeof(TagViewModel));
            presenter.Show(request);
            presenter.
                */
        }
    }
}
