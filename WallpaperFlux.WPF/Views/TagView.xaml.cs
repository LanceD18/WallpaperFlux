using MvvmCross.Platforms.Wpf.Presenters.Attributes;
using MvvmCross.Platforms.Wpf.Views;
using MvvmCross.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
using WallpaperFlux.Core.Models.Controls;
using WallpaperFlux.Core.Models.Tagging;
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
            //? The below may be re-implemented in the future is the bug associated with it is fixed
            //x TaggingUtil.SetInstance(TagViewModel.Instance);
        }

        private void CategoryTabControl_OnSizeChanged(object sender, SizeChangedEventArgs e) => UpdateTagSelectorWrapperSize();

        private void CategoryTabControl_OnSelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateTagSelectorWrapperSize();

        private void UpdateTagSelectorWrapperSize()
        {
            TagViewModel viewModel = (TagViewModel)this.DataContext;

            if (viewModel.SelectedCategory?.SelectedTagTab != null)
            {
                double widthOffset = 25; // pushes the wrap cutoff closer as the right-side can also be cut off
                double heightOffset = 170; // the bottom tends to be cut off so we need an offset

                viewModel.SelectedCategory.SelectedTagTab.SetTagWrapSize(CategoryTabControl.ActualWidth - widthOffset, CategoryTabControl.ActualHeight - heightOffset);
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

        private void GroupBox_Tag_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // TODO Remove me
        }

        private void TagView_OnSizeChanged_UpdateTagBoardHeight(object sender, SizeChangedEventArgs e) => ((TagViewModel)this.DataContext).SetTagBoardHeight(ActualHeight - 75);

        private void TagTabControl_ListBoxItem_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (ControlUtil.UsingSingularSelection()) //? Placing this check ahead of time to avoid the additional processing time
            {
                CategoryModel selectedCategory = CategoryTabControl.SelectedItem as CategoryModel;

                // get all tabs in all categories
                List<TagTabModel> tagTabs = new List<TagTabModel>();
                foreach (CategoryModel category in CategoryTabControl.Items)
                {
                    tagTabs.AddRange(category.TagTabs.ToList());
                }

                ControlUtil.EnsureSingularSelection(tagTabs.ToArray(), selectedCategory.SelectedTagTab);
            }

        }
    }
}
