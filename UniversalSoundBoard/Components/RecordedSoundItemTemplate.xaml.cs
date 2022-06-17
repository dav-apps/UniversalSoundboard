using System;
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

        public RecordedSoundItemTemplate()
        {
            InitializeComponent();
            DataContextChanged += (s, e) => Bindings.Update();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            RecordedSoundItem.AudioPlayerStarted += RecordedSoundItem_AudioPlayerStarted;
            RecordedSoundItem.AudioPlayerPaused += RecordedSoundItem_AudioPlayerPaused;
        }

        private async void RecordedSoundItem_AudioPlayerStarted(object sender, EventArgs e)
        {
            await MainPage.dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                PlayPauseButton.Content = "\uE62E";
            });
        }

        private async void RecordedSoundItem_AudioPlayerPaused(object sender, EventArgs e)
        {
            await MainPage.dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                PlayPauseButton.Content = "\uF5B0";
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
    }
}
