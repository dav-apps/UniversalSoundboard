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
            throw new NotImplementedException();
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

    public class PlayingSoundsBarVisibilityConverter : IValueConverter
    {
        // This is bound to the acrylic background StackPanel in the NavigationViewHeader
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return FileManager.itemViewHolder.PlayingSoundsListVisible && FileManager.itemViewHolder.Page == typeof(SoundPage);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }
    }

    public class CollapsedButtonsWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if ((string)parameter == "small")
                return (bool)value ? 40 : 100;
            else
                return (bool)value ? 40 : 140;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return 60;
        }
    }

    public class OptionButtonVisibleAndMultiSelectionEnabledVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return !(bool)value && FileManager.itemViewHolder.MultiSelectionEnabled;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class OptionButtonVisibleAndMultiSelectionDisabledVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return !(bool)value && !FileManager.itemViewHolder.MultiSelectionEnabled;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
                foreach (var category in categories)
                    icons += " " + category.Icon;
            else
                foreach (var category in categories)
                    icons += category.Icon + "\n\n";

            return icons;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }
    }
}
