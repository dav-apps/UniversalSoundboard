using System.Threading.Tasks;
using UniversalSoundboard.Common;
using UniversalSoundboard.DataAccess;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Dialogs
{
    public class Dialog
    {
        protected ContentDialog ContentDialog { get; }
        public object Content
        {
            get => ContentDialog?.Content;
            protected set
            {
                if (ContentDialog == null) return;
                ContentDialog.Content = value;
            }
        }

        public event TypedEventHandler<Dialog, ContentDialogButtonClickEventArgs> PrimaryButtonClick;
        public event TypedEventHandler<Dialog, ContentDialogButtonClickEventArgs> CloseButtonClick;

        public Dialog()
        {
            ContentDialog = new ContentDialog();

            ContentDialog.PrimaryButtonClick += (ContentDialog sender, ContentDialogButtonClickEventArgs args) => PrimaryButtonClick?.Invoke(this, args);
            ContentDialog.CloseButtonClick += (ContentDialog sender, ContentDialogButtonClickEventArgs args) => CloseButtonClick?.Invoke(this, args);
        }

        public Dialog(
            string title,
            string closeButtonText
        )
        {
            ContentDialog = new ContentDialog
            {
                Title = title,
                CloseButtonText = closeButtonText,
                RequestedTheme = FileManager.GetRequestedTheme()
            };

            ContentDialog.CloseButtonClick += (ContentDialog sender, ContentDialogButtonClickEventArgs args) => CloseButtonClick?.Invoke(this, args);
        }

        public Dialog(
            string title,
            string primaryButtonText,
            string closeButtonText,
            ContentDialogButton defaultButton = ContentDialogButton.Primary
        ) : this(
            title,
            null,
            primaryButtonText,
            closeButtonText,
            defaultButton
        ) { }

        public Dialog(
            string title,
            object content,
            string primaryButtonText,
            string closeButtonText,
            ContentDialogButton defaultButton = ContentDialogButton.Primary
        )
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

            ContentDialog.PrimaryButtonClick += (ContentDialog sender, ContentDialogButtonClickEventArgs args) => PrimaryButtonClick?.Invoke(this, args);
            ContentDialog.CloseButtonClick += (ContentDialog sender, ContentDialogButtonClickEventArgs args) => CloseButtonClick?.Invoke(this, args);
        }

        public async Task ShowAsync(AppWindowType appWindowType = AppWindowType.Main)
        {
            await ContentDialogs.ShowContentDialogAsync(ContentDialog, appWindowType);
        }
    }
}
