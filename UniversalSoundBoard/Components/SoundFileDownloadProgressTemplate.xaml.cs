using davClassLibrary;
using System;
using UniversalSoundboard.Models;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Components
{
    public sealed partial class SoundFileDownloadProgressTemplate : UserControl
    {
        Sound Sound { get; set; }

        public SoundFileDownloadProgressTemplate()
        {
            InitializeComponent();
            DataContextChanged += SoundFileDownloadProgressTemplate_DataContextChanged;
        }

        private void SoundFileDownloadProgressTemplate_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (DataContext == null) return;

            Sound = DataContext as Sound;
            Bindings.Update();

            // Schedule the file download
            var downloadStatus = Sound.GetAudioFileDownloadStatus();

            if (
                downloadStatus == TableObjectFileDownloadStatus.Downloading
                || downloadStatus == TableObjectFileDownloadStatus.NotDownloaded
            )
            {
                DownloadProgressBar.IsIndeterminate = true;

                Sound.AudioFileTableObject.ScheduleFileDownload(new Progress<(Guid, int)>(DownloadProgress));
            }
            else
            {
                DownloadProgressBar.Visibility = Visibility.Collapsed;
            }
        }

        private void RetryDownloadButton_Click(object sender, RoutedEventArgs e)
        {
            // Reset the progress bar
            DownloadProgressBar.ShowError = false;
            DownloadProgressBar.IsIndeterminate = true;
            RetryDownloadButton.Visibility = Visibility.Collapsed;

            // Try to download the file again
            Sound.AudioFileTableObject.ScheduleFileDownload(new Progress<(Guid, int)>(DownloadProgress));
        }

        private void DownloadProgress((Guid, int) value)
        {
            if (value.Item2 < 0)
            {
                // There was an error
                DownloadProgressBar.ShowError = true;

                // Show the button for retrying the download
                RetryDownloadButton.Visibility = Visibility.Visible;
            }
            else if (value.Item2 >= 0 && value.Item2 <= 100)
            {
                DownloadProgressBar.IsIndeterminate = false;
                DownloadProgressBar.Value = value.Item2;
            }
        }
    }
}
