using System;
using System.Collections.Generic;
using UniversalSoundboard.Common;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Dialogs;
using UniversalSoundboard.Models;
using UniversalSoundboard.Pages;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Components
{
    public sealed partial class RecordedSoundItemTemplate : UserControl
    {
        public RecordedSoundItem RecordedSoundItem { get { return DataContext as RecordedSoundItem; } }

        private string recordedSoundLengthText = "";

        public RecordedSoundItemTemplate()
        {
            InitializeComponent();
            DataContextChanged += RecordedSoundItemTemplate_DataContextChanged;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            PlayPauseButtonTooltip.Text = FileManager.loader.GetString("PlayButtonToolTip");
        }

        private async void RecordedSoundItemTemplate_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (RecordedSoundItem == null) return;

            RecordedSoundItem.AudioPlayerStarted -= RecordedSoundItem_AudioPlayerStarted;
            RecordedSoundItem.AudioPlayerStarted += RecordedSoundItem_AudioPlayerStarted;
            RecordedSoundItem.AudioPlayerPaused -= RecordedSoundItem_AudioPlayerPaused;
            RecordedSoundItem.AudioPlayerPaused += RecordedSoundItem_AudioPlayerPaused;

            TimeSpan duration = await RecordedSoundItem.GetDuration();
            recordedSoundLengthText = string.Format("{0:D2}:{1:D2}", duration.Minutes, duration.Seconds);

            Bindings.Update();
        }

        private async void RecordedSoundItem_AudioPlayerStarted(object sender, EventArgs e)
        {
            await MainPage.dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                PlayPauseButton.Content = "\uE62E";
                PlayPauseButtonTooltip.Text = FileManager.loader.GetString("PauseButtonToolTip");
            });
        }

        private async void RecordedSoundItem_AudioPlayerPaused(object sender, EventArgs e)
        {
            await MainPage.dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                PlayPauseButton.Content = "\uF5B0";
                PlayPauseButtonTooltip.Text = FileManager.loader.GetString("PlayButtonToolTip");
            });
        }

        private async void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            // Check if there are any output devices connected
            if (
                !RecordedSoundItem.IsPlaying
                && FileManager.deviceWatcherHelper.Devices.Count == 0
            )
            {
                var noAudioDeviceDialog = new NoAudioDeviceDialog();
                await noAudioDeviceDialog.ShowAsync(AppWindowType.SoundRecorder);
                return;
            }

            if (
                RecordedSoundItem.IsPlaying
                && RecordedSoundItem.Pause()
            ) PlayPauseButton.Content = "\uF5B0";
            else if (
                !RecordedSoundItem.IsPlaying
                && await RecordedSoundItem.Play()
            ) PlayPauseButton.Content = "\uE62E";
        }

        private async void AddToSoundboardButton_Click(object sender, RoutedEventArgs e)
        {
            var addToSoundboardDialog = new AddRecordedSoundToSoundboardDialog(RecordedSoundItem.Name);
            addToSoundboardDialog.PrimaryButtonClick += AddToSoundboardContentDialog_PrimaryButtonClick;
            await addToSoundboardDialog.ShowAsync(AppWindowType.SoundRecorder);
        }

        private async void AddToSoundboardContentDialog_PrimaryButtonClick(Dialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Get the name of the sound
            string soundName = (sender as AddRecordedSoundToSoundboardDialog).Name;

            // Create the sound
            Guid uuid = await FileManager.CreateSoundAsync(null, soundName, new List<Guid>(), RecordedSoundItem.File);

            // Add the sound to the list of sounds
            await FileManager.AddSound(uuid);

            // Remove the recorded sound from the recorder window
            await RecordedSoundItem.Remove();
        }

        private async void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            var removeRecordedSoundDialog = new RemoveRecordedSoundDialog(RecordedSoundItem.Name);
            removeRecordedSoundDialog.PrimaryButtonClick += RemoveRecordedSoundContentDialog_PrimaryButtonClick;
            await removeRecordedSoundDialog.ShowAsync(AppWindowType.SoundRecorder);
        }

        private async void RemoveRecordedSoundContentDialog_PrimaryButtonClick(Dialog sender, ContentDialogButtonClickEventArgs args)
        {
            await RecordedSoundItem.Remove();
        }
    }
}
