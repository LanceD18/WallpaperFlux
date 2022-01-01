using MvvmCross.Platforms.Wpf.Presenters.Attributes;
using MvvmCross.Platforms.Wpf.Views;
using MvvmCross.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MvvmCross.Presenters;
using WallpaperFlux.Core.ViewModels;
using WallpaperFlux.WPF.Windows;

namespace WallpaperFlux.WPF.Views
{
    /// <summary>
    /// Interaction logic for TagView.xaml
    /// </summary>
    [MvxContentPresentation (WindowIdentifier = nameof(TagWindow))]
    [MvxViewFor(typeof(TagViewModel))]
    public partial class TagView : MvxWpfView
    {
        public TagView()
        {
            InitializeComponent();
            ViewModel = new TagViewModel();

            //xMvxWpfViewPresenter presenter = new MvxWpfViewPresenter(this);

            /*x
            MvxContentPresentationAttribute attribute = new MvxContentPresentationAttribute();
            attribute.WindowIdentifier = nameof(TagWindow);
            ViewPresenter presenter = new ViewPresenter(new TagWindow(), attribute, typeof(TagViewModel));
            */
        }
    }
}
