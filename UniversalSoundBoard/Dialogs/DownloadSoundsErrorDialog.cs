using System;
using System.Collections.Generic;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Models;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Dialogs
{
    public class DownloadSoundsErrorDialog : Dialog
    {
        public DownloadSoundsErrorDialog(List<SoundDownloadItem> soundItems)
            : base(
                  FileManager.loader.GetString("DownloadSoundsErrorDialog-Title"),
                  FileManager.loader.GetString("Actions-Close")
            )
        {
            Content = GetContent(soundItems);
        }

        private StackPanel GetContent(List<SoundDownloadItem> soundItems)
        {
            StackPanel containerStackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            TextBlock descriptionTextBlock = new TextBlock
            {
                Text = FileManager.loader.GetString("DownloadSoundsErrorDialog-Description"),
                Margin = new Thickness(0, 0, 0, 8)
            };

            containerStackPanel.Children.Add(descriptionTextBlock);

            ScrollViewer scrollViewer = new ScrollViewer
            {
                MaxHeight = 300
            };

            StackPanel scrollViewerContainerStackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            foreach (var soundItem in soundItems)
            {
                scrollViewerContainerStackPanel.Children.Add(
                    new HyperlinkButton
                    {
                        Content = soundItem.Name != null ? soundItem.Name : soundItem.AudioFileUrl,
                        NavigateUri = new Uri(soundItem.AudioFileUrl)
                    }
                );
            }

            scrollViewer.Content = scrollViewerContainerStackPanel;
            containerStackPanel.Children.Add(scrollViewer);

            return containerStackPanel;
        }
    }
}
