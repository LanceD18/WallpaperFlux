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
using HandyControl.Controls;
using MvvmCross.Presenters;
using WallpaperFlux.Core.Models.Controls;
using WallpaperFlux.Core.Models.Tagging;
using WallpaperFlux.Core.ViewModels;
using WallpaperFlux.WPF.Util;
using WallpaperFlux.WPF.Windows;
using WallpaperFlux.Core.Util;
using ControlUtil = WallpaperFlux.WPF.Util.ControlUtil;
using TabItem = System.Windows.Controls.TabItem;
using TextBox = System.Windows.Controls.TextBox;

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

            //! [Now initialized through the constructor of WindowUtil] ViewModel = TagViewModel.Instance = WindowUtil.InitializeViewModel(TagViewModel.Instance);
            ViewModel = TagViewModel.Instance;

            //? The below may be re-implemented in the future if the bug associated with it is fixed
            //x TaggingUtil.SetInstance(TagViewModel.Instance);
            Debug.WriteLine("Opened"); //? this constructor essentially acts an an on-open event, so we'll use it to select the first category on opening
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

        private void TagView_OnSizeChanged_UpdateDrawerHeight(object sender, SizeChangedEventArgs e) => ((TagViewModel)this.DataContext).SetDrawerHeight(ActualHeight - 75);

        // this captures the selection range of the entire listbox item
        private void TagTabControl_ListBoxItem_OnPreviewMouseDown(object sender, MouseButtonEventArgs e)
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

        #region Drag n Drop
        // Help: https://stackoverflow.com/questions/10738161/is-it-possible-to-rearrange-tab-items-in-tab-control-in-wpf
        private void TabItem_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (Mouse.PrimaryDevice.LeftButton == MouseButtonState.Pressed && e.Source is TabItem tabItem)
            {
                DragDrop.DoDragDrop(tabItem, tabItem, DragDropEffects.All);
            }
        }

        private void TabItem_Drop(object sender, DragEventArgs e)
        {
            // target, or e.Sourcem is the item the drop lands on
            // source, or data, is the item we are dragging

            if (e.Source is TabItem tabItemTarget &&
                e.Data.GetData(typeof(TabItem)) is TabItem tabItemSource &&
                !tabItemTarget.Equals(tabItemSource)  /*x&&
               tabItemTarget.Parent is TabControl tabControl*/)
            {
                if (tabItemSource.Header is CategoryModel sourceCategory &&
                    tabItemTarget.Header is CategoryModel targetCategory)
                {
                    Debug.WriteLine("Dropped");

                    Debug.WriteLine("Source: " + sourceCategory.Name);
                    Debug.WriteLine("Target: " + targetCategory.Name);

                    //xint targetIndex = tabControl.Items.IndexOf(tabItemTarget);

                    //xint sourceIndex = CategoryTabControl.Items.IndexOf(tabItemTarget);
                    //xint targetIndex = CategoryTabControl.Items.IndexOf(tabItemSource);

                    //xIEnumerable<TabItem> tabItems = CategoryTabControl.Items;

                    TaggingUtil.ShiftCategories(sourceCategory, targetCategory);

                    //xtabControl.Items.Remove(tabItemSource);
                    //xtabControl.Items.Insert(targetIndex, tabItemSource);
                    tabItemSource.IsSelected = true;
                }
                else
                {
                    Debug.WriteLine("Invalid");
                }
            }
            else
            {
                Debug.WriteLine("Self or Invalid");
            }
        }


        // Help: https://stackoverflow.com/questions/3350187/wpf-c-rearrange-items-in-listbox-via-drag-and-drop
        private void ListBoxItem_FolderPriority_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && sender is ListBoxItem draggedItem)
            {
                DragDrop.DoDragDrop(draggedItem, draggedItem.DataContext, DragDropEffects.Move);
                draggedItem.IsSelected = true;
            }
        }

        private void ListBoxItem_FolderPriority_Drop(object sender, DragEventArgs e)
        {
            if (e.Source is ContentPresenter contentPresenterTarget &&
                e.Data.GetData(typeof(FolderPriorityModel)) is FolderPriorityModel sourcePriority)
            {
                if (contentPresenterTarget.Content is FolderPriorityModel targetPriority)
                {
                    if (!targetPriority.Equals(sourcePriority))
                    {
                        Debug.WriteLine("Dropped");

                        Debug.WriteLine("Source: " + sourcePriority.Name);
                        Debug.WriteLine("Target: " + targetPriority.Name);

                        TaggingUtil.ShiftPriorities(sourcePriority, targetPriority);

                        sourcePriority.IsSelected = true;
                    }
                    else
                    {
                        Debug.WriteLine("Self");
                    }
                }
                else
                {
                    Debug.WriteLine("Invalid Target");
                }
            }
            else
            {
                Debug.WriteLine("Invalid Source or Target");
            }
        }

        #endregion
    }
}
