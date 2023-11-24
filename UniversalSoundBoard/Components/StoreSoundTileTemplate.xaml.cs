using MimeTypes;
using System;
using System.Linq;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Dialogs;
using UniversalSoundboard.Models;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Components
{
    public sealed partial class StoreSoundTileTemplate : UserControl
    {
        private bool isPlaying = false;
        private double width = 250;

        public SoundResponse SoundItem { get; set; }
        public new double Width
        {
            get => width;
            set
            {
                width = value;
            }
        }

        public event EventHandler<EventArgs> Play;
        public event EventHandler<EventArgs> Pause;

        public StoreSoundTileTemplate()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            UpdatePlayPauseButtonUI();
        }

        private void UserControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (DataContext == null) return;

            SoundItem = DataContext as SoundResponse;
            Bindings.Update();
        }

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (isPlaying)
                Pause?.Invoke(this, EventArgs.Empty);
            else
                Play?.Invoke(this, EventArgs.Empty);

            isPlaying = !isPlaying;
            UpdatePlayPauseButtonUI();
        }

        private void UpdatePlayPauseButtonUI()
        {
            if (isPlaying)
            {
                PlayPauseButton.Content = "\uE103";
                PlayPauseButtonToolTip.Text = FileManager.loader.GetString("PauseButtonToolTip");
            }
            else
            {
                PlayPauseButton.Content = "\uE102";
                PlayPauseButtonToolTip.Text = FileManager.loader.GetString("PlayButtonToolTip");
            }
        }

        public void PlaybackStopped()
        {
            isPlaying = false;
            UpdatePlayPauseButtonUI();
        }

        private async void SelectSoundButton_Click(object sender, RoutedEventArgs e)
        {
            var itemTemplate = Resources["DialogSoundListItemTemplate"] as DataTemplate;

            var soundSelectionDialog = new SoundSelectionDialog(FileManager.itemViewHolder.AllSounds.ToList(), itemTemplate);
            soundSelectionDialog.PrimaryButtonClick += SoundSelectionDialog_PrimaryButtonClick;
            await soundSelectionDialog.ShowAsync();
        }

        private async void SoundSelectionDialog_PrimaryButtonClick(Dialog sender, ContentDialogButtonClickEventArgs args)
        {
            var soundSelectionDialog = sender as SoundSelectionDialog;
            DialogSoundListItem selectedSoundItem = soundSelectionDialog.SelectedSoundItem;
            
            string mimeType = "audio/mpeg";

            try
            {
                mimeType = MimeTypeMap.GetMimeType(selectedSoundItem.Sound.AudioFileTableObject.GetPropertyValue("ext"));
            }
            catch (Exception) { }

            await ApiManager.UploadSoundFile(SoundItem.Uuid, selectedSoundItem.Sound.AudioFile, mimeType);
        }
    }
}
