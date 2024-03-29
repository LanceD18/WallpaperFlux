﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;
using WallpaperFlux.Core.Models;
using WallpaperFlux.Core.Models.Controls;
using WallpaperFlux.Core.ViewModels;

namespace WallpaperFlux.WPF.Util
{
    public static class ControlUtil
    {
        public static bool UsingSingularSelection() => Keyboard.Modifiers != ModifierKeys.Control && Keyboard.Modifiers != ModifierKeys.Shift;

        #region Tab Control

        //? Deselect items from other tabs if a normal selection is made
        // We are not looking to select multiple items if a singular selection is made, clear selections from other pages
        // Without this other pages will retain their selection as they have separate ListBoxes
        private static void ApplySingularSelectionDeselect<T, U>(IEnumerable<T> items, ITabModel<U> selectedItem)
        {
            Debug.WriteLine("Singular Selection | Deselecting Other Tabs");
            foreach (ITabModel<U> tab in items)
            {
                if (tab != selectedItem) //? the WPF will handle deselecting the selected item itself (without deselecting the singular selection)
                {
                    //x Debug.WriteLine("Tab Deselected");
                    tab.DeselectAllItems();
                }
            }
        }

        public static void EnsureSingularSelection<T, U>(IEnumerable<T> items, ITabModel<U> selectedItem)
        {
            if (UsingSingularSelection())
            {
                ApplySingularSelectionDeselect(items, selectedItem);
            }
        }

        public static void EnsureSingularSelection<T, U>(ItemCollection itemCollection, ITabModel<U> selectedItem)
        {
            if (UsingSingularSelection())
            {
                IEnumerable<T> items = itemCollection.OfType<T>().ToArray();

                ApplySingularSelectionDeselect(items, selectedItem);
            }
        }

        #endregion
    }
}
