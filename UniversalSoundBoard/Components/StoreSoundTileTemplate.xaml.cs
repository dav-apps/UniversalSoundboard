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
        public SoundResponse SoundItem { get; set; }
        private bool isPlaying = false;

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
            await soundSelectionDialog.ShowAsync();
        }
    }
}
