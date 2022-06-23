using System.Collections.Generic;

namespace WallpaperFlux.Core.IoC
{
    public interface IExternalDisplayUtil
    {
        int GetDisplayCount();

        void ResetLargestDisplayIndexOrder();

        IEnumerable<int> GetLargestDisplayIndexOrder();
    }
}
