using UniversalSoundboard.DataAccess;
using Windows.UI.Text;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Documents;

namespace UniversalSoundboard.Dialogs
{
    public class StartSoundPromotionDialog : Dialog
    {
        public StartSoundPromotionDialog()
            : base(
                  FileManager.loader.GetString("StartSoundPromotionDialog-Title"),
                  FileManager.loader.GetString("StartSoundPromotionDialog-PrimaryButton"),
                  FileManager.loader.GetString("Actions-Close")
                )
        {
            Content = GetContent();
        }

        private StackPanel GetContent()
        {
            StackPanel contentStackPanel = new StackPanel();

            RichTextBlock descriptionTextBlock = new RichTextBlock
            {
                TextWrapping = TextWrapping.WrapWholeWords
            };

            Paragraph paragraph1 = new Paragraph();

            paragraph1.Inlines.Add(new Run
            {
                Text = FileManager.loader.GetString("StartSoundPromotionDialog-Intro")
            });

            Paragraph paragraph2 = new Paragraph
            {
                Margin = new Thickness(0, 12, 0, 0)
            };

            paragraph2.Inlines.Add(new Run
            {
                Text = FileManager.loader.GetString("StartSoundPromotionDialog-HowItWorks")
            });

            Paragraph paragraph3 = new Paragraph
            {
                Margin = new Thickness(8, 12, 0, 0)
            };

            paragraph3.Inlines.Add(new Run
            {
                Text = "📈 "
            });
            paragraph3.Inlines.Add(new Run
            {
                Text = FileManager.loader.GetString("StartSoundPromotionDialog-FirstPoint-Head"),
                FontWeight = FontWeights.SemiBold
            });
            paragraph3.Inlines.Add(new Run
            {
                Text = FileManager.loader.GetString("StartSoundPromotionDialog-FirstPoint-Text")
            });

            Paragraph paragraph4 = new Paragraph
            {
                Margin = new Thickness(8, 12, 0, 0)
            };

            paragraph4.Inlines.Add(new Run
            {
                Text = "🔥 "
            });
            paragraph4.Inlines.Add(new Run
            {
                Text = FileManager.loader.GetString("StartSoundPromotionDialog-SecondPoint-Head"),
                FontWeight = FontWeights.SemiBold
            });
            paragraph4.Inlines.Add(new Run
            {
                Text = FileManager.loader.GetString("StartSoundPromotionDialog-SecondPoint-Text")
            });

            descriptionTextBlock.Blocks.Add(paragraph1);
            descriptionTextBlock.Blocks.Add(paragraph2);
            descriptionTextBlock.Blocks.Add(paragraph3);
            descriptionTextBlock.Blocks.Add(paragraph4);

            contentStackPanel.Children.Add(descriptionTextBlock);

            return contentStackPanel;
        }
    }
}
