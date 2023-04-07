using System.Windows;
using System.Windows.Controls;

namespace WallpaperFlux.WPF.CustomControl
{
    // http://dotnetgui.blogspot.com/2013/03/contentcontrol-contentchangedevent.html
    public class ChangeAwareContentControl : ContentControl
    {
        static ChangeAwareContentControl()
        {
            ContentProperty.OverrideMetadata(typeof(ChangeAwareContentControl),
                new FrameworkPropertyMetadata(
                    new PropertyChangedCallback(OnContentChanged)));
        }

        private static void OnContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ChangeAwareContentControl mcc = d as ChangeAwareContentControl;
            if (mcc.ContentChanged != null)
            {
                DependencyPropertyChangedEventArgs args
                    = new DependencyPropertyChangedEventArgs(
                        ContentProperty, e.OldValue, e.NewValue);

                mcc.ContentChanged(mcc, args);
            }
        }

        public event DependencyPropertyChangedEventHandler ContentChanged;
    }
}
