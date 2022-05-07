using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using MvvmCross.ViewModels;
using WallpaperFlux.Core.Models.Controls;

namespace WallpaperFlux.Core.Util
{
    public static class ControlUtil
    {
        public static void VerifyListBoxCollectionChange<T>(NotifyCollectionChangedEventArgs args, MvxObservableCollection<T> collection)
        {
            if (args.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (T item in args.NewItems)
                {
                    if (item != null)
                    {
                        (item as ListBoxItemModel).IsSelected = false; // if the selection was cleared while this item was selected its selection status will stay true into the next selection
                    }
                    else
                    {
                        //? This could happen in the event that, say for instance, the user selects an image that doesn't exist in the theme, it would not be found and be sent as null
                        //? Allowing this to be managed here instead of the source allows us to avoid all possible related scenarios with just this line
                        Debug.WriteLine("Invalid item found, removing");
                        collection.Remove(item);
                    }
                }
            }
        }
    }
}
