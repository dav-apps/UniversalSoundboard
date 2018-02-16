using System;
using System.Collections.Generic;
using System.Linq;
using UniversalSoundBoard.DataAccess;
using UniversalSoundBoard.Models;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

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
            throw new NotImplementedException();
        }
    }

    public class CollapsedButtonsWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if((string)parameter == "small")
                return (bool)value ? 50 : 100;
            else
                return (bool)value ? 50 : 140;
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
            return (App.Current as App)._itemViewHolder.normalOptionsVisibility ? (bool)value : false;
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
            return (App.Current as App)._itemViewHolder.normalOptionsVisibility ? false : (bool)value;
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
            return (App.Current as App)._itemViewHolder.categories[(App.Current as App)._itemViewHolder.selectedCategory];
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
