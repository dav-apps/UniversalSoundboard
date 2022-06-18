using System;
using UniversalSoundboard.DataAccess;
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
            DataContextChanged += (s, e) => Bindings.Update();
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (RecordedSoundItem == null) return;

            RecordedSoundItem.AudioPlayerStarted += RecordedSoundItem_AudioPlayerStarted;
            RecordedSoundItem.AudioPlayerPaused += RecordedSoundItem_AudioPlayerPaused;

            PlayPauseButtonTooltip.Text = FileManager.loader.GetString("PlayButtonToolTip");

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
            if (RecordedSoundItem.IsPlaying)
            {
                RecordedSoundItem.Pause();
                PlayPauseButton.Content = "\uF5B0";
            }
            else
            {
                await RecordedSoundItem.Play();
                PlayPauseButton.Content = "\uE62E";
            }
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            RecordedSoundItem.Remove();
        }
    }
}
