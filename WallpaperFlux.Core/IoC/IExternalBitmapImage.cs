namespace WallpaperFlux.Core.IoC
{
    // Registered with the Mvx IoCProvider
    public interface IExternalBitmapImage
    {
        void InitCompressedSource(string imagePath, int width, int height);
    }
}
