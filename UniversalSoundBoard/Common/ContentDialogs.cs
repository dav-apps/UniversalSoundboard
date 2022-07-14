using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UniversalSoundboard.Models;
using UniversalSoundboard.Pages;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Common
{
    public class ContentDialogs
    {
        #region Variables
        private static List<KeyValuePair<AppWindowType, ContentDialog>> contentDialogQueue = new List<KeyValuePair<AppWindowType, ContentDialog>>();

        private static bool _contentDialogVisible = false;
        public static bool ContentDialogVisible { get => _contentDialogVisible; }

        public static TextBox DownloadSoundsUrlTextBox;
        public static string DownloadSoundsAudioFileName = "";
        public static string DownloadSoundsAudioFileType = "";
        public static List<Sound> downloadingFilesSoundsList = new List<Sound>();
        public static ContentDialog DownloadFilesContentDialog;
        #endregion

        #region General methods
        public static async Task ShowContentDialogAsync(ContentDialog contentDialog, AppWindowType appWindowType = AppWindowType.Main)
        {
            contentDialog.Closed += async (e, s) =>
            {
                int i = contentDialogQueue.FindIndex(pair => pair.Value == contentDialog);

                if (i == -1)
                {
                    _contentDialogVisible = false;
                }
                else
                {
                    contentDialogQueue.RemoveAt(i);

                    if (contentDialogQueue.Count > 0)
                    {
                        // Show the next content dialog
                        _contentDialogVisible = true;
                        await contentDialogQueue.First().Value.ShowAsync();
                    }
                    else
                    {
                        _contentDialogVisible = false;
                    }
                }
            };

            contentDialogQueue.Add(new KeyValuePair<AppWindowType, ContentDialog>(appWindowType, contentDialog));

            if (appWindowType == AppWindowType.SoundRecorder && MainPage.soundRecorderAppWindowContentFrame != null)
                contentDialog.XamlRoot = MainPage.soundRecorderAppWindowContentFrame.XamlRoot;

            if (!_contentDialogVisible)
            {
                _contentDialogVisible = true;
                await contentDialog.ShowAsync();
            }
        }
        #endregion
    }
}
