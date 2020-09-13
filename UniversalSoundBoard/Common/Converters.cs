using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniversalSoundBoard.DataAccess;
using UniversalSoundBoard.Models;
using UniversalSoundBoard.Pages;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media.Imaging;

namespace UniversalSoundBoard.Converters
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

    public class CategoryIconsConverter : IValueConverter
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

    public class LogoImageSourceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return FileManager.itemViewHolder.CurrentTheme == FileManager.AppTheme.Light ? "ms-appx:///Assets/Icons/altform-lightunplated/Square44x44Logo.scale-400.png" : "ms-appx:///Assets/Icons/altform-unplated/Square44x44Logo.scale-400.png";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }

    public class AppStateLoadingOrInitialSyncConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return
                FileManager.itemViewHolder.AppState == FileManager.AppState.Loading
                || FileManager.itemViewHolder.AppState == FileManager.AppState.InitialSync;
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
}
