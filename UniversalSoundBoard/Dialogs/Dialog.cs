using System.Threading.Tasks;
using UniversalSoundboard.Common;
using UniversalSoundboard.DataAccess;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Dialogs
{
    public class Dialog
    {
        private ContentDialog ContentDialog { get; }
        public event TypedEventHandler<ContentDialog, ContentDialogButtonClickEventArgs> PrimaryButtonClick;
        public event TypedEventHandler<ContentDialog, ContentDialogButtonClickEventArgs> CloseButtonClick;

        public Dialog(string title, string content, string closeButtonText)
        {
            ContentDialog = new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = closeButtonText,
                RequestedTheme = FileManager.GetRequestedTheme()
            };

            ContentDialog.CloseButtonClick += (ContentDialog sender, ContentDialogButtonClickEventArgs args) => CloseButtonClick?.Invoke(sender, args);
        }

        public Dialog(string title, string content, string primaryButtonText, string closeButtonText, ContentDialogButton defaultButton = ContentDialogButton.Primary)
        {
            ContentDialog = new ContentDialog
            {
                Title = title,
                Content = content,
                PrimaryButtonText = primaryButtonText,
                CloseButtonText = closeButtonText,
                DefaultButton = defaultButton,
                RequestedTheme = FileManager.GetRequestedTheme()
            };

            ContentDialog.PrimaryButtonClick += (ContentDialog sender, ContentDialogButtonClickEventArgs args) => PrimaryButtonClick?.Invoke(sender, args);
            ContentDialog.CloseButtonClick += (ContentDialog sender, ContentDialogButtonClickEventArgs args) => CloseButtonClick?.Invoke(sender, args);
        }

        public async Task ShowAsync()
        {
            await ContentDialogs.ShowContentDialogAsync(ContentDialog);
        }
    }
}
