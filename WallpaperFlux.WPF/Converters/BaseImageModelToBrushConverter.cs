using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;
using WallpaperFlux.Core.Models;

namespace WallpaperFlux.WPF.Converters
{
    class BaseImageModelToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                BaseImageModel image = (BaseImageModel)value;

                if (image is ImageModel iModel)
                {
                    if (iModel.IsGif)
                    {
                        Color color = Colors.SeaGreen;
                        color.A = 200;
                        return new SolidColorBrush(color);
                    }
                    else if (iModel.IsVideo)
                    {
                        return new SolidColorBrush(Color.FromArgb(200, 205, 0, 0));
                    }
                }
                else if (image is ImageSetModel)
                {
                    return new SolidColorBrush(Colors.SlateBlue);
                }

                return new SolidColorBrush(Colors.Transparent);
            }
            catch (Exception e)
            {
                throw new ArgumentException("BaseImageModel required for this conversion");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
