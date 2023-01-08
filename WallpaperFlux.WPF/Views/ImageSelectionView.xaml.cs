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
using MvvmCross.Platforms.Wpf.Views;
using WallpaperFlux.Core.ViewModels;
using WallpaperFlux.WPF.Util;

namespace WallpaperFlux.WPF.Views
{
    /// <summary>
    /// Interaction logic for SelectImagesView.xaml
    /// </summary>
    public partial class ImageSelectionView : MvxWpfView
    {
        public ImageSelectionView()
        {
            InitializeComponent();

            //! [Now initialized through the constructor of WindowUtil] ViewModel = SettingsViewModel.Instance = WindowUtil.InitializeViewModel(SettingsViewModel.Instance);
            ViewModel = ImageSelectionViewModel.Instance;
        }
    }
}
