using System;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Models;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Dialogs
{
    public class PropertiesDialog : Dialog
    {
        public PropertiesDialog(Sound sound, ulong audioFileSize = 0, ulong imageFileSize = 0)
            : base(
                  FileManager.loader.GetString("SoundItemOptionsFlyout-Properties"),
                  FileManager.loader.GetString("Actions-Close")
            )
        {
            Content = GetContent(sound, audioFileSize, imageFileSize);
        }

        private Grid GetContent(Sound sound, ulong audioFileSize, ulong imageFileSize)
        {
            int fontSize = 15;
            int row = 0;
            int contentGridWidth = 500;
            int leftColumnWidth = 210;

            Grid contentGrid = new Grid { Width = contentGridWidth };

            // Create the columns
            var firstColumn = new ColumnDefinition { Width = new GridLength(leftColumnWidth, GridUnitType.Pixel) };
            var secondColumn = new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) };

            contentGrid.ColumnDefinitions.Add(firstColumn);
            contentGrid.ColumnDefinitions.Add(secondColumn);

            #region Name
            // Add the row
            var nameRow = new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) };
            contentGrid.RowDefinitions.Add(nameRow);

            StackPanel nameHeaderStackPanel = GenerateTableCell(
                row,
                0,
                FileManager.loader.GetString("PropertiesDialog-Name"),
                fontSize,
                false,
                null
            );

            StackPanel nameDataStackPanel = GenerateTableCell(
                row,
                1,
                sound.Name,
                fontSize,
                true,
                null
            );

            row++;
            contentGrid.Children.Add(nameHeaderStackPanel);
            contentGrid.Children.Add(nameDataStackPanel);
            #endregion

            #region Source
            if (sound.Source != null)
            {
                // Add the row
                var sourceRow = new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) };
                contentGrid.RowDefinitions.Add(sourceRow);

                StackPanel sourceHeaderStackPanel = GenerateTableCell(
                    row,
                    0,
                    FileManager.loader.GetString("PropertiesDialog-Source"),
                    fontSize,
                    false,
                    null
                );

                StackPanel sourceDataStackPanel = new StackPanel();
                Grid.SetRow(sourceDataStackPanel, row);
                Grid.SetColumn(sourceDataStackPanel, 1);

                Uri sourceUrl = new Uri(sound.Source);

                HyperlinkButton hyperlinkButton = new HyperlinkButton {
                    Content = sourceUrl.Host,
                    NavigateUri = sourceUrl,
                    Margin = new Thickness(0, 10, 0, 0),
                    Padding = new Thickness(0, 1, 0, 1)
                };
                sourceDataStackPanel.Children.Add(hyperlinkButton);

                row++;
                contentGrid.Children.Add(sourceHeaderStackPanel);
                contentGrid.Children.Add(sourceDataStackPanel);
            }
            #endregion

            #region File type
            string audioFileType = sound.GetAudioFileExtension();

            if (audioFileType != null)
            {
                // Add the row
                var fileTypeRow = new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) };
                contentGrid.RowDefinitions.Add(fileTypeRow);

                StackPanel fileTypeHeaderStackPanel = GenerateTableCell(
                    row,
                    0,
                    FileManager.loader.GetString("PropertiesDialog-FileType"),
                    fontSize,
                    false,
                    null
                );

                StackPanel fileTypeDataStackPanel = GenerateTableCell(
                    row,
                    1,
                    audioFileType,
                    fontSize,
                    true,
                    null
                );

                row++;
                contentGrid.Children.Add(fileTypeHeaderStackPanel);
                contentGrid.Children.Add(fileTypeDataStackPanel);
            }
            #endregion

            #region Image file type
            string imageFileType = sound.GetImageFileExtension();

            if (imageFileType != null)
            {
                // Add the row
                var imageFileTypeRow = new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) };
                contentGrid.RowDefinitions.Add(imageFileTypeRow);

                StackPanel imageFileTypeHeaderStackPanel = GenerateTableCell(
                    row,
                    0,
                    FileManager.loader.GetString("PropertiesDialog-ImageFileType"),
                    fontSize,
                    false,
                    null
                );

                StackPanel imageFileTypeDataStackPanel = GenerateTableCell(
                    row,
                    1,
                    imageFileType,
                    fontSize,
                    true,
                    null
                );

                row++;
                contentGrid.Children.Add(imageFileTypeHeaderStackPanel);
                contentGrid.Children.Add(imageFileTypeDataStackPanel);
            }
            #endregion

            #region Size
            var audioFile = sound.AudioFile;

            if (audioFile != null)
            {
                // Add the row
                var sizeRow = new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) };
                contentGrid.RowDefinitions.Add(sizeRow);

                StackPanel sizeHeaderStackPanel = GenerateTableCell(
                    row,
                    0,
                    FileManager.loader.GetString("PropertiesDialog-Size"),
                    fontSize,
                    false,
                    null
                );

                StackPanel sizeDataStackPanel = GenerateTableCell(
                    row,
                    1,
                    FileManager.GetFormattedSize(audioFileSize),
                    fontSize,
                    true,
                    null
                );

                row++;
                contentGrid.Children.Add(sizeHeaderStackPanel);
                contentGrid.Children.Add(sizeDataStackPanel);
            }
            #endregion

            #region Image size
            var imageFile = sound.ImageFile;

            if (imageFile != null)
            {
                // Add the row
                var imageSizeRow = new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) };
                contentGrid.RowDefinitions.Add(imageSizeRow);

                StackPanel imageSizeHeaderStackPanel = GenerateTableCell(
                    row,
                    0,
                    FileManager.loader.GetString("PropertiesDialog-ImageSize"),
                    fontSize,
                    false,
                    null
                );

                StackPanel imageSizeDataStackPanel = GenerateTableCell(
                    row,
                    1,
                    FileManager.GetFormattedSize(imageFileSize),
                    fontSize,
                    true,
                    null
                );

                row++;
                contentGrid.Children.Add(imageSizeHeaderStackPanel);
                contentGrid.Children.Add(imageSizeDataStackPanel);
            }
            #endregion

            return contentGrid;
        }

        private StackPanel GenerateTableCell(
            int row,
            int column,
            string text,
            int fontSize,
            bool isTextSelectionEnabled,
            Thickness? margin
        )
        {
            StackPanel contentStackPanel = new StackPanel();
            Grid.SetRow(contentStackPanel, row);
            Grid.SetColumn(contentStackPanel, column);

            TextBlock contentTextBlock = new TextBlock
            {
                Text = text,
                Margin = margin ?? new Thickness(0, 10, 0, 0),
                FontSize = fontSize,
                TextWrapping = TextWrapping.WrapWholeWords,
                IsTextSelectionEnabled = isTextSelectionEnabled
            };

            contentStackPanel.Children.Add(contentTextBlock);
            return contentStackPanel;
        }
    }
}
