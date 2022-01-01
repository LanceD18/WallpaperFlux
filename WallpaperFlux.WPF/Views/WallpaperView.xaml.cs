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
using System.Windows.Navigation;
using System.Windows.Shapes;
using MvvmCross.Platforms.Wpf.Presenters.Attributes;
using MvvmCross.Platforms.Wpf.Views;
using MvvmCross.ViewModels;
using WallpaperFlux.Core.Util;

namespace WallpaperFlux.WPF.Views
{
    // TODO I don't think this is being used, pretty sure WallpaperWindow was the official outcome, remove me
    /// <summary>
    /// Interaction logic for WallpaperView.xaml
    /// </summary>
    [MvxContentPresentation]
    [MvxViewFor(typeof(WallpaperView))]
    public partial class WallpaperView : MvxWpfView
    {
        public WallpaperView()
        {
            InitializeComponent();
        }
    }
}
