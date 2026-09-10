using Windows.UI.Xaml;

namespace RichTextControls.Generators
{
    public delegate void SelectionChangedEventHandler(object sender, RoutedEventArgs e);
    public interface IHtmlXamlGenerator
    {
        UIElement Generate();

        Style BlockquoteBorderStyle { get; set; }

        Style PreformattedBorderStyle { get; set; }

        event SelectionChangedEventHandler SelectionChanged;
    }
}
