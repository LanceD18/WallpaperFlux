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

            if (viewModel.SelectedCategory?.SelectedTagTab != null)
            {
                double widthOffset = 25; // pushes the wrap cutoff closer as the right-side can also be cut off
                double heightOffset = 170; // the bottom tends to be cut off so we need an offset

                viewModel.SelectedCategory.SelectedTagTab.SetTagWrapSize(TagTabControl.ActualWidth - widthOffset, TagTabControl.ActualHeight - heightOffset);
            }
        }

        private void SearchBar_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // The update trigger for the Search Bar is explicit, so we need to use UpdateSource()
                // this allows us to control when we update the tags (Minimizing lag)
                (sender as TextBox).GetBindingExpression(TextBox.TextProperty).UpdateSource();
            }
        }
    }
}
