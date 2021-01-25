using System;
using UniversalSoundboard.Models;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using static davClassLibrary.Models.TableObject;

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
                DownloadProgressBar.Visibility = Visibility.Visible;
                DownloadProgressBar.IsIndeterminate = true;

                Sound.AudioFileTableObject.ScheduleFileDownload(new Progress<(Guid, int)>(DownloadProgress));
            }
            else
            {
                DownloadProgressBar.Visibility = Visibility.Collapsed;
            }
        }

        private void DownloadProgress((Guid, int) value)
        {
            if(value.Item2 < 0)
            {
                // There was an error
                DownloadProgressBar.Visibility = Visibility.Collapsed;
            }
            else if (value.Item2 > 100)
            {
                // Download was successful
                DownloadProgressBar.Visibility = Visibility.Collapsed;
            }
            else
            {
                DownloadProgressBar.Visibility = Visibility.Visible;
                DownloadProgressBar.IsIndeterminate = false;
                DownloadProgressBar.Value = value.Item2;
            }
        }
    }
}
