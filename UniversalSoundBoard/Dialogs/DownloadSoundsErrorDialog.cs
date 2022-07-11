using System;
using System.Collections.Generic;
using UniversalSoundboard.DataAccess;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Dialogs
{
    public class DownloadSoundsErrorDialog : Dialog
    {
        public DownloadSoundsErrorDialog(List<KeyValuePair<string, string>> soundsList)
            : base(
                  FileManager.loader.GetString("DownloadSoundsErrorDialog-Title"),
                  FileManager.loader.GetString("Actions-Close")
            )
        {
            Content = GetContent(soundsList);
        }

        private StackPanel GetContent(List<KeyValuePair<string, string>> soundsList)
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

            foreach (var soundItem in soundsList)
            {
                scrollViewerContainerStackPanel.Children.Add(
                    new HyperlinkButton
                    {
                        Content = soundItem.Key != null ? soundItem.Key : soundItem.Value,
                        NavigateUri = new Uri(soundItem.Value)
                    }
                );
            }

            scrollViewer.Content = scrollViewerContainerStackPanel;
            containerStackPanel.Children.Add(scrollViewer);

            return containerStackPanel;
        }
    }
}
