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

namespace WallpaperFlux.WPF.Views
{
    /// <summary>
    /// Interaction logic for PaginationTestView.xaml
    /// </summary>
    [MvxViewFor(typeof(PaginationTestView))]
    public partial class PaginationTestView : MvxWpfView
    {
        public PaginationTestView()
        {
            InitializeComponent();
        }
    }
}
