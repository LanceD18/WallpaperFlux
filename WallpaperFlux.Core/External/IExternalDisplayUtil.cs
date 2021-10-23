using System;
using System.Collections.Generic;
using System.Text;

namespace WallpaperFlux.Core.External
{
    public interface IExternalDisplayUtil
    {
        int GetDisplayCount();

        void ResetLargestDisplayIndexOrder();

        IEnumerable<int> GetLargestDisplayIndexOrder();
    }
}
