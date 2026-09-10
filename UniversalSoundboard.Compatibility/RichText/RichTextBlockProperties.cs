using RichTextControls.Generators;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;

namespace RichTextControls
{
    public sealed class Properties : DependencyObject
    {
        //public static DependencyProperty HtmlProperty { get { return DependencyProperty.RegisterAttached("Html", typeof(string), typeof(Properties), new PropertyMetadata(null, HtmlChanged)); } }

        public static readonly DependencyProperty HtmlProperty =
            DependencyProperty.Register("MyProperty", typeof(string), typeof(Properties), new PropertyMetadata(null, HtmlChanged));

        public static void SetHtml(DependencyObject obj, string value)
        {
            obj.SetValue(HtmlProperty, value);
        }

        public static string GetHtml(DependencyObject obj)
        {
            return (string)obj.GetValue(HtmlProperty);
        }

        private static void HtmlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is RichTextBlock richText)) return;

            RichTextBlock newRichText = RenderDocument(e.NewValue.ToString());

            richText.Blocks.Clear();

            for (int i = newRichText.Blocks.Count - 1; i >= 0; i--)
            {
                Block b = newRichText.Blocks[i];
                newRichText.Blocks.RemoveAt(i);
                richText.Blocks.Insert(0, b);
            }
        }

        static private RichTextBlock RenderDocument(string Html)
        {
            if (String.IsNullOrEmpty(Html))
                return new RichTextBlock();

            var generator = new HtmlXamlGenerator(Html);
            return generator.Generate() as RichTextBlock;
        }


    }
}
