using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UniversalSoundboard.Common;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Pages;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Dialogs
{
    public class Dialog
    {
        public Guid Uuid { get; private set; }
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

        private static List<KeyValuePair<AppWindowType, Dialog>> dialogQueue = new List<KeyValuePair<AppWindowType, Dialog>>();
        public static Dialog CurrentlyVisibleDialog { get; private set; }

        public Dialog()
        {
            Uuid = Guid.NewGuid();
            ContentDialog = new ContentDialog
            {
                Tag = Uuid
            };

            ContentDialog.PrimaryButtonClick += (ContentDialog sender, ContentDialogButtonClickEventArgs args) => PrimaryButtonClick?.Invoke(this, args);
            ContentDialog.CloseButtonClick += (ContentDialog sender, ContentDialogButtonClickEventArgs args) => CloseButtonClick?.Invoke(this, args);
        }

        public Dialog(ContentDialog contentDialog)
        {
            Uuid = Guid.NewGuid();
            ContentDialog = contentDialog;
            ContentDialog.Tag = Uuid;
        }

        public Dialog(
            string title,
            string closeButtonText
        )
        {
            Uuid = Guid.NewGuid();

            ContentDialog = new ContentDialog
            {
                Title = title,
                CloseButtonText = closeButtonText,
                RequestedTheme = FileManager.GetRequestedTheme(),
                Tag = Uuid
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
            Uuid = Guid.NewGuid();

            ContentDialog = new ContentDialog
            {
                Title = title,
                Content = content,
                PrimaryButtonText = primaryButtonText,
                CloseButtonText = closeButtonText,
                DefaultButton = defaultButton,
                RequestedTheme = FileManager.GetRequestedTheme(),
                Tag = Uuid
            };

            ContentDialog.PrimaryButtonClick += (ContentDialog sender, ContentDialogButtonClickEventArgs args) => PrimaryButtonClick?.Invoke(this, args);
            ContentDialog.CloseButtonClick += (ContentDialog sender, ContentDialogButtonClickEventArgs args) => CloseButtonClick?.Invoke(this, args);
        }

        public async Task ShowAsync(AppWindowType appWindowType = AppWindowType.Main)
        {
            ContentDialog.Closed += async (e, s) =>
            {
                // Remove the closed dialog from the queue
                var closedDialogUuid = (Guid)e.Tag;
                int i = dialogQueue.FindIndex(pair => pair.Value.Uuid.Equals(closedDialogUuid));

                if (i != -1)
                    dialogQueue.RemoveAt(i);

                if (dialogQueue.Count > 0)
                {
                    // Show the next dialog in the queue
                    var nextDialog = dialogQueue.First().Value;
                    CurrentlyVisibleDialog = nextDialog;
                    await nextDialog.ContentDialog.ShowAsync();
                }
                else
                {
                    CurrentlyVisibleDialog = null;
                }
            };

            dialogQueue.Add(new KeyValuePair<AppWindowType, Dialog>(appWindowType, this));

            if (appWindowType == AppWindowType.SoundRecorder && MainPage.soundRecorderAppWindowContentFrame != null)
                ContentDialog.XamlRoot = MainPage.soundRecorderAppWindowContentFrame.XamlRoot;

            if (CurrentlyVisibleDialog == null)
            {
                CurrentlyVisibleDialog = this;
                await ContentDialog.ShowAsync();
            }
        }

        public void Hide()
        {
            ContentDialog.Hide();
        }
    }
}
