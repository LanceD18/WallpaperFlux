using WallpaperFlux.Core.Collections;

namespace WallpaperFlux.Core.Models.Theme
{
    //? Making this a regular class instead of a static class will make resetting its data easier on you
    public class ThemeModel
    {
        public SettingsModel Settings; // for options

        public ImageList Images;

        public FolderList Folders;
    }
}
