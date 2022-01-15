using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Models;
using UniversalSoundboard.Pages;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;

namespace UniversalSoundboard.Converters
{
    public class ValueConverterGroup : List<IValueConverter>, IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return this.Aggregate(value, (current, converter) => converter.Convert(current, targetType, parameter, language));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }

    public class InvertBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return !(bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return !(bool)value;
        }
    }

    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (bool)value ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class BooleanToInvertedVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (bool)value ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class FileToBitmapImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            // Get FileInfo and return BitmapImage
            if (!(value is FileInfo file)) return null;
            return new BitmapImage(new Uri(file.FullName));
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            // Get BitmapImage and return FileInfo
            var bitmapImage = value as BitmapImage;
            return new FileInfo(bitmapImage.UriSource.AbsolutePath);
        }
    }

    public class OptionsOnSoundPageVisibleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value as Type == typeof(SoundPage) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }

    public class BooleanToSelectionMode : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (bool)value ? ListViewSelectionMode.Multiple : ListViewSelectionMode.None;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }

    public class CategoriesIconsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null) return "";

            List<Category> categories = value as List<Category>;
            string icons = "";

            if((string)parameter == "list")
            {
                for(int i = 0; i < categories.Count; i++)
                {
                    if (i >= 5) break;
                    icons += " " + categories[i].Icon;
                }
            }
            else
            {
                for(int i = 0; i < categories.Count; i++)
                {
                    if (i >= 4) break;
                    icons += categories[i].Icon + "\n\n";
                }
            }

            return icons;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }

    public class CategoriesMarginConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var categories = value as List<Category>;

            if (categories.Count == 0) return new Thickness(0, 0, 0, 0);
            return new Thickness(0, 0, 10, 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }

    public class TopButtonWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (bool)value ? 40 : DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }

    public class TopButtonPaddingConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (bool)value ? new Thickness(0) : new Thickness(10, 0, 10, 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }

    public class GridViewReorderItemsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return FileManager.itemViewHolder.SoundOrder == FileManager.SoundOrder.Custom;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }

    public class TopOfThicknessToGridLengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return new GridLength(((Thickness)value).Top, GridUnitType.Pixel);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class BottomOfThicknessToGridLengthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return new GridLength(((Thickness)value).Bottom, GridUnitType.Pixel);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class LogoImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return FileManager.itemViewHolder.CurrentTheme == FileManager.AppTheme.Light ? "ms-appx:///Assets/Images/usb-logo.png" : "ms-appx:///Assets/Images/usb-logo-white.png";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }

    public class AppStateLoadingConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return FileManager.itemViewHolder.AppState == FileManager.AppState.Loading;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }

    public class AppStateNormalConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return FileManager.itemViewHolder.AppState == FileManager.AppState.Normal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }

    public class MediaElementSliderTooltipConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            int totalSeconds = System.Convert.ToInt32(value);

            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;

            return $"{minutes:D2}:{seconds:D2}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class DavPlanToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            int plan = (int)value;

            switch (plan)
            {
                case 1:
                    return "Plus";
                case 2:
                    return "Pro";
                default:
                    return "Free";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class DavPlanFreeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (int)value == 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
