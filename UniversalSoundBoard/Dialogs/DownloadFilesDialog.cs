using System.Collections.Generic;
using System.Collections.ObjectModel;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Models;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Dialogs
{
    public class DownloadFilesDialog : Dialog
    {
        public ObservableCollection<Sound> Sounds { get; private set; }
        public bool DownloadCanceled { get; private set; }

        public DownloadFilesDialog(
            List<Sound> sounds,
            DataTemplate itemTemplate,
            Style itemStyle
        ) : base(
                  FileManager.loader.GetString("DownloadFilesDialog-Title"),
                  FileManager.loader.GetString("Actions-Cancel")
            )
        {
            Sounds = new ObservableCollection<Sound>(sounds);
            Content = GetContent(itemTemplate, itemStyle);

            DownloadCanceled = false;
            CloseButtonClick += DownloadFilesDialog_CloseButtonClick;
        }

        private Grid GetContent(DataTemplate itemTemplate, Style itemStyle)
        {
            ListView progressListView = new ListView
            {
                ItemTemplate = itemTemplate,
                ItemsSource = Sounds,
                ItemContainerStyle = itemStyle,
                SelectionMode = ListViewSelectionMode.None
            };

            Grid containerGrid = new Grid
            {
                Width = 500
            };

            containerGrid.Children.Add(progressListView);
            return containerGrid;
        }

        private void DownloadFilesDialog_CloseButtonClick(Dialog sender, ContentDialogButtonClickEventArgs args)
        {
            DownloadCanceled = true;
        }
    }
}
