using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Dialogs
{
    public class StoreAddToSoundboardDialog : Dialog
    {
        public StoreAddToSoundboardDialog()
            : base("Add to soundboard", "Add", "Cancel", ContentDialogButton.Primary)
        {
            Content = GetContent();
        }

        private StackPanel GetContent()
        {
            StackPanel contentPanel = new StackPanel();
            return contentPanel;
        }
    }
}
