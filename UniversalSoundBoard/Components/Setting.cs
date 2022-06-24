using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Components
{
    public class Setting : ContentControl
    {
        public Setting()
        {
            DefaultStyleKey = typeof(Setting);
            Style = (Style)Application.Current.Resources["SettingStyle"];
        }

        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register("Header", typeof(string), typeof(Setting), null);
        public static readonly DependencyProperty ActionContentProperty = DependencyProperty.Register("ActionContent", typeof(object), typeof(Setting), null);

        public string Header
        {
            get => (string)GetValue(HeaderProperty);
            set => SetValue(HeaderProperty, value);
        }

        public object ActionContent
        {
            get => GetValue(ActionContentProperty);
            set => SetValue(ActionContentProperty, value);
        }
    }
}
