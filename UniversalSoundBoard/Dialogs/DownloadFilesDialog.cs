﻿using System.Collections.Generic;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Models;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Dialogs
{
    public class DownloadFilesDialog : Dialog
    {
        public DownloadFilesDialog(
            List<Sound> sounds,
            DataTemplate itemTemplate,
            Style itemStyle
        ) : base(
                  FileManager.loader.GetString("DownloadFilesDialog-Title"),
                  FileManager.loader.GetString("Actions-Cancel")
            )
        {
            Content = GetContent(sounds, itemTemplate, itemStyle);
        }

        private Grid GetContent(List<Sound> sounds, DataTemplate itemTemplate, Style itemStyle)
        {
            ListView progressListView = new ListView
            {
                ItemTemplate = itemTemplate,
                ItemsSource = sounds,
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
    }
}