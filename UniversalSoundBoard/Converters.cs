using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace UniversalSoundBoard
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
            return (value as string).ToUpper();
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
}
