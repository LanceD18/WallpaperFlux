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
using WallpaperFlux.WPF.Util;
using WallpaperFlux.WPF.Windows;

namespace WallpaperFlux.WPF.Views
{
    /// <summary>
    /// Interaction logic for TagView.xaml
    /// </summary>
    //x[MvxContentPresentation (WindowIdentifier = nameof(TagWindow))]
    [MvxViewFor(typeof(TagViewModel))]
    public partial class TagView : MvxWpfView
    {
        public TagView()
        {
            InitializeComponent();

            ViewModel = TagViewModel.Instance = WindowUtil.InitializeViewModel(TagViewModel.Instance);

            /*x
            if (TagViewModel.Instance == null)
            {
                ViewModel = TagViewModel.Instance = new TagViewModel();
            }
            else
            {
                ViewModel = TagViewModel.Instance;
            }
            */
        }

        private void TagTabControl_OnSizeChanged(object sender, SizeChangedEventArgs e) => UpdateTagSelectorWrapperSize();

        private void TagTabControl_OnSelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateTagSelectorWrapperSize();

        private void UpdateTagSelectorWrapperSize()
        {
            TagViewModel viewModel = (TagViewModel)this.DataContext;

            if (viewModel.SelectedCategory != null)
            {
                viewModel.SelectedCategory.TagWrapWidth = TagTabControl.ActualWidth;
                viewModel.SelectedCategory.TagWrapHeight = TagTabControl.ActualHeight - 100; // the bottom tends to be cut off
                viewModel.SelectedCategory.RaisePropertyChanged(() => viewModel.SelectedCategory.TagWrapWidth);
                viewModel.SelectedCategory.RaisePropertyChanged(() => viewModel.SelectedCategory.TagWrapHeight);
            }
        }
    }
}
