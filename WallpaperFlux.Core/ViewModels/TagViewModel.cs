using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Dynamic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using MvvmCross.Commands;
using MvvmCross.ViewModels;
using WallpaperFlux.Core.Collections;
using WallpaperFlux.Core.Models;
using WallpaperFlux.Core.Models.Tagging;
using WallpaperFlux.Core.Util;

namespace WallpaperFlux.Core.ViewModels
{
    public class TagViewModel : MvxViewModel
    {
        public static TagViewModel Instance; // allows the data to remain persistent without having to reload everything once the view is closed

        #region View Variables

        // Categories
        private MvxObservableCollection<CategoryModel> _categories = new MvxObservableCollection<CategoryModel>();
        public MvxObservableCollection<CategoryModel> Categories
        {
            get => _categories;
            set => SetProperty(ref _categories, value);
        }

        // Selected Category
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

        #region TagBoard
        private MvxObservableCollection<TagModel> _tagBoardTags = new MvxObservableCollection<TagModel>();

        public MvxObservableCollection<TagModel> TagBoardTags
        {
            get => _tagBoardTags;
            set => SetProperty(ref _tagBoardTags, value);
        }

        private double _tagBoardHeight;
        public double TagBoardHeight
        {
            get => _tagBoardHeight;
            set => SetProperty(ref _tagBoardHeight, value); //? needed to update the height when resizing the window
        }

        #endregion

        #region Minor UI Settings
        public double WindowBorderThickness => (TagAdderToggle || TagRemoverToggle) ? 5 : 0;
        public Color WindowBorderBrushColor => TagAdderToggle ? Color.LimeGreen : Color.Red;
        #endregion

        #endregion

        #region Enablers

        public bool CategoryIsSelected => SelectedCategory != null;

        public bool CategoriesExist => Categories.Count > 0;

        private bool _tagAdderToggle = false;
        public bool TagAdderToggle
        {
            get => _tagAdderToggle; 
            set
            {
                SetProperty(ref _tagAdderToggle, value);

                if (value == true)
                {
                    TagRemoverToggle = false; // toggles off the opposing toggle, we don't want both of them active at the same time
                    RaisePropertyChanged(() => TagRemoverToggle);
                }

                RaisePropertyChanged(() => WindowBorderThickness);
                RaisePropertyChanged(() => WindowBorderBrushColor);
                RaisePropertyChanged(() => EditingTagsOfAnImage);
            }
        }

        private bool _tagRemoverToggle = false;

        public bool TagRemoverToggle
        {
            get => _tagRemoverToggle;
            set
            {
                SetProperty(ref _tagRemoverToggle, value);

                if (value == true)
                {
                    TagAdderToggle = false; // toggles off the opposing toggle, we don't want both of them active at the same time
                    RaisePropertyChanged(() => TagAdderToggle);
                }

                RaisePropertyChanged(() => WindowBorderThickness);
                RaisePropertyChanged(() => WindowBorderBrushColor);
                RaisePropertyChanged(() => EditingTagsOfAnImage);
            }
        }

        private bool _tagboardToggle;

        public bool TagboardToggle
        {
            get => _tagboardToggle;
            set => SetProperty(ref _tagboardToggle, value);
        }

        public bool EditingTagsOfAnImage => TagAdderToggle || TagRemoverToggle;

        #endregion

        #region Commands

        public IMvxCommand AddCategoryCommand { get; set; }

        public IMvxCommand AddTagToSelectedCategoryCommand { get; set; }

        public IMvxCommand CloseTagBoardCommand { get; set; }

        #endregion

        // TODO Add a ToolTip explaining how Category Order determines the order of image-naming
        public TagViewModel()
        {
            AddCategoryCommand = new MvxCommand(PromptAddCategory);
            AddTagToSelectedCategoryCommand = new MvxCommand(() => PromptAddTagToCategory(SelectedCategory));
            //? the open/toggle TagBoard is initially called by CategoryModel and sent to a method here
            CloseTagBoardCommand = new MvxCommand(() => TagboardToggle = false);
        }

        public void HighlightTags(TagCollection tags)
        {
            TagModel[] visibleTags = SelectedCategory.SelectedTagTab.GetAllItems();
            
            foreach (TagModel tag in visibleTags)
            {
                tag.IsHighlighted = tags.Contains(tag);
            }
        }

        #region TagBoard

        public void ToggleTagBoard() => TagboardToggle = !TagboardToggle;

        public void AddTagsToTagBoard(TagModel[] tags) //! Range actions { AddRange() } are not supported for observable collections so we must do this manually
        {
            foreach (TagModel tag in tags)
            {
                if (!TagBoardTags.Contains(tag))
                {
                    TagBoardTags.Add(tag);
                }
            }
        }

        public void ClearTagBoardTags() => TagBoardTags.Clear();

        public void RemoveTagFromTagBoard(TagModel tag) => TagBoardTags.Remove(tag);

        public void SetTagBoardHeight(double newHeight) => TagBoardHeight = newHeight;

        #endregion

        #region Command Methods
        public void PromptAddCategory()
        {
            string categoryName = MessageBoxUtil.GetString("Create New Category", "Give a name for this category", "Category Name...");

            if (!string.IsNullOrEmpty(categoryName))
            {
                CategoryModel category = new CategoryModel(categoryName);
                Categories.Add(category);
                SelectedCategory = category;
                RaisePropertyChanged(() => CategoriesExist);
                RaisePropertyChanged(() => SelectedCategory);
            }
        }

        public void PromptAddTagToCategory(CategoryModel category)
        {
            if (category != null)
            {
                string tagName = MessageBoxUtil.GetString("Create New Tag", "Give a name for this tag", "Tag Name...");

                if (!string.IsNullOrEmpty(tagName))
                {
                    category.AddTag(new TagModel(tagName));
                    AddDebugTags(category);
                }
            }
            else
            {
                MessageBoxUtil.ShowError("Selected category does not exist");
            }
        }

        //? for testing the tagging system [TODO: REMOVE LATER]
        private void AddDebugTags(CategoryModel category)
        {
            Debug.WriteLine("Adding Debug Tags...");
            //! temp debug code for generating a bunch of random tags
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            Random charRand = new Random();
            Random intRand = new Random();

            int nextCount = intRand.Next(50, 100);

            TagModel[] tagsToAdd = new TagModel[nextCount];

            for (int i = 0; i < nextCount; i++)
            {
                int nextSize = intRand.Next(10, 15);
                char[] stringChars = new char[nextSize];

                for (int j = 0; j < stringChars.Length; j++)
                {
                    stringChars[j] = chars[charRand.Next(chars.Length)];
                }

                string finalString = new string(stringChars);
                tagsToAdd[i] = new TagModel(finalString);
            }

            category.AddTagRange(tagsToAdd);
            //! temp debug code
        }
        #endregion
    }
}