using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UniversalSoundBoard.DataAccess;
using UniversalSoundBoard.Models;
using UniversalSoundBoard.Pages;
using Windows.UI.Xaml;
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

    public class CutTitleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string newTitle = (string)value;
            if (string.IsNullOrEmpty(newTitle))
                return "";

            double width = Window.Current.Bounds.Width;
            int maxLength = 20;

            if (width < FileManager.hideSearchBoxMaxWidth)
                maxLength = 14;
            else if (width > FileManager.topButtonsCollapsedMaxWidth)
                maxLength = 23;
            else if (width > FileManager.topButtonsCollapsedMaxWidth * 1.5)
                maxLength = 28;
            else if (width > FileManager.topButtonsCollapsedMaxWidth * 2.5)
                maxLength = 35;

            if(newTitle.Count() > maxLength)
            {
                newTitle = newTitle.Substring(0, maxLength);
                newTitle = newTitle.Insert(newTitle.Count(), "...");
            }
            return newTitle;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return (App.Current as App)._itemViewHolder.Title;
        }
    }

    public class CollapsedButtonsWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if((string)parameter == "small")
                return (bool)value ? 48 : 100;
            else
                return (bool)value ? 48 : 140;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return 60;
        }
    }

    public class ReverseBoolConverter : IValueConverter
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

    public class MakeBoolFalseIfSelectOptionsVisible : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            // Make more button normal options flyout entries invisible if select options are visible
            return (App.Current as App)._itemViewHolder.NormalOptionsVisibility ? (bool)value : false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class MakeBoolFalseIfNormalOptionsVisible : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            // Make more button select options flyout entries invisible if normal options are visible
            return (App.Current as App)._itemViewHolder.NormalOptionsVisibility ? false : (bool)value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class ReturnValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value as Category;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value as Category;
        }
    }

    public class SelectedCategoryConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            // Get the index and return the category from the categories list at the index
            int index = (int) value;

            if((App.Current as App)._itemViewHolder.Categories.Count > index)
                return (App.Current as App)._itemViewHolder.Categories[index];
            else if((App.Current as App)._itemViewHolder.Categories.Count == 0)
                return null;
            else
                return (App.Current as App)._itemViewHolder.Categories[0];
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            // Get the category and return the index
            var category = value as Category;

            int i = 0;
            foreach(Category cat in (App.Current as App)._itemViewHolder.Categories)
            {
                if (cat == category)
                    return i;
                i++;
            }
            return 0;
        }
    }

    public class FileToBitmapImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            // Get FileInfo and return BitmapImage
            FileInfo file = value as FileInfo;
            if (file == null) return null;
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
            var playingSoundsBarVisibility = (App.Current as App)._itemViewHolder.PlayingSoundsListVisibility;
            var page = (App.Current as App)._itemViewHolder.Page;
            
            return playingSoundsBarVisibility == Visibility.Visible && page == typeof(SoundPage);
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

            foreach (var category in categories)
                icons += category.Icon + " ";

            return icons;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
