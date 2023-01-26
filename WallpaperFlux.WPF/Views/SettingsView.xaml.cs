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
using MvvmCross.ViewModels;
using WallpaperFlux.Core.ViewModels;
using WallpaperFlux.WPF.Util;

namespace WallpaperFlux.WPF.Views
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    [MvxViewFor(typeof(SettingsViewModel))]
    public partial class SettingsView : MvxWpfView
    {
        public SettingsView()
        {
            InitializeComponent();

            //! [Now initialized through the constructor of WindowUtil] ViewModel = SettingsViewModel.Instance = WindowUtil.InitializeViewModel(SettingsViewModel.Instance);
            ViewModel = SettingsViewModel.Instance;
        }
    }
}
