using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using WallpaperFlux.Core.Models.Tagging;
using WallpaperFlux.Core.Util;

namespace WallpaperFlux.Core.ViewModels
{
    public class TagViewModel : MvxViewModel
    {
        public static TagViewModel Instance; // allows the data to remain persistent without having to reload everything

        #region View Variables

        // Categories
        private MvxObservableCollection<CategoryModel> _categories = new MvxObservableCollection<CategoryModel>();

        public MvxObservableCollection<CategoryModel> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        private CategoryModel _selectedCategory;
        public CategoryModel SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                SetProperty(ref _selectedCategory, value);
                RaisePropertyChanged(() => CategoryIsSelected);
            }
        }

        #endregion

        #region Enablers

        public bool CategoryIsSelected => SelectedCategory != null;

        public bool CategoriesExist => Categories.Count > 0;

        #endregion

        #region Commands

        public IMvxCommand AddCategoryCommand { get; set; }

        public IMvxCommand AddTagToSelectCategoryCommand { get; set; }

        #endregion

        // TODO Add a ToolTip explaining how Category Order determines the order of image-naming
        public TagViewModel()
        {
            AddCategoryCommand = new MvxCommand(PromptAddCategory);
            AddTagToSelectCategoryCommand = new MvxCommand(() => PromptAddTag(SelectedCategory));
        }

        #region Command Methods
        public void PromptAddCategory()
        {
            string categoryName = MessageBoxUtil.GetString("Create New Category", "Give a name for this category", "Category Name...");

            if (!string.IsNullOrEmpty(categoryName))
            {
                Categories.Add(new CategoryModel(categoryName));
                RaisePropertyChanged(() => CategoriesExist);
            }
        }

        public void PromptAddTag(CategoryModel category)
        {
            if (category != null)
            {
                string tagName = MessageBoxUtil.GetString("Create New Tag", "Give a name for this tag", "Tag Name...");

                if (!string.IsNullOrEmpty(tagName))
                {
                    category.Tags.Add(new TagModel(tagName));
                }
            }
            else
            {
                MessageBoxUtil.ShowError("Selected category does not exist");
            }
        }
        #endregion
    }
}