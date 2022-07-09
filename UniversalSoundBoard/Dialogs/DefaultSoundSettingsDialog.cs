using System.Collections.Generic;
using UniversalSoundboard.Components;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Models;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace UniversalSoundboard.Dialogs
{
    public class DefaultSoundSettingsDialog : Dialog
    {
        private Sound sound;
        private VolumeControl VolumeControl;
        private ComboBox RepetitionsComboBox;
        private ComboBox PlaybackSpeedComboBox;
        private bool volumeChanged = false;
        private bool mutedChanged = false;
        private bool repetitionsChanged = false;

        public DefaultSoundSettingsDialog(Sound sound)
            : base(
                  string.Format(FileManager.loader.GetString("DefaultSoundSettingsDialog-Title"), sound.Name),
                  FileManager.loader.GetString("Actions-Close")
            )
        {
            this.sound = sound;
            Content = GetContent();
            ContentDialog.CloseButtonClick += ContentDialog_CloseButtonClick;
        }

        private Grid GetContent()
        {
            int fontSize = 15;
            int row = 0;
            int contentGridWidth = 500;
            int leftColumnWidth = 210;
            int rightColumnWidth = contentGridWidth - leftColumnWidth;

            Grid contentGrid = new Grid { Width = contentGridWidth };

            // Create the columns
            var firstColumn = new ColumnDefinition { Width = new GridLength(leftColumnWidth, GridUnitType.Pixel) };
            var secondColumn = new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) };

            contentGrid.ColumnDefinitions.Add(firstColumn);
            contentGrid.ColumnDefinitions.Add(secondColumn);

            #region Description
            // Add the row
            var descriptionRow = new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) };
            contentGrid.RowDefinitions.Add(descriptionRow);

            StackPanel descriptionStackPanel = new StackPanel();
            Grid.SetRow(descriptionStackPanel, row);
            Grid.SetColumn(descriptionStackPanel, 0);
            Grid.SetColumnSpan(descriptionStackPanel, 2);

            TextBlock descriptionTextBlock = new TextBlock
            {
                Text = FileManager.loader.GetString("DefaultSoundSettingsDialog-Description"),
                Margin = new Thickness(0, 0, 0, 0),
                FontSize = fontSize,
                TextWrapping = TextWrapping.WrapWholeWords
            };

            descriptionStackPanel.Children.Add(descriptionTextBlock);

            row++;
            contentGrid.Children.Add(descriptionStackPanel);
            #endregion

            #region Volume
            // Add the row
            var volumeRow = new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) };
            contentGrid.RowDefinitions.Add(volumeRow);

            StackPanel volumeHeaderStackPanel = GenerateTableCell(
                row,
                0,
                FileManager.loader.GetString("DefaultSoundSettingsDialog-Volume"),
                fontSize,
                false,
                new Thickness(0, 16, 0, 0)
            );

            StackPanel volumeDataStackPanel = new StackPanel();
            Grid.SetRow(volumeDataStackPanel, row);
            Grid.SetColumn(volumeDataStackPanel, 1);

            RelativePanel volumeRelativePanel = new RelativePanel();
            volumeDataStackPanel.Children.Add(volumeRelativePanel);

            VolumeControl = new VolumeControl
            {
                Value = sound.DefaultVolume,
                Muted = sound.DefaultMuted,
                Margin = new Thickness(8, 10, 0, 0)
            };
            VolumeControl.ValueChanged += VolumeControl_ValueChanged;
            VolumeControl.MuteChanged += VolumeControl_MuteChanged;

            volumeRelativePanel.Children.Add(VolumeControl);

            row++;
            contentGrid.Children.Add(volumeHeaderStackPanel);
            contentGrid.Children.Add(volumeDataStackPanel);
            #endregion

            #region Repetitions
            // Add the row
            var repetitionsRow = new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) };
            contentGrid.RowDefinitions.Add(repetitionsRow);

            StackPanel repetitionsHeaderStackPanel = GenerateTableCell(
                row,
                0,
                FileManager.loader.GetString("DefaultSoundSettingsDialog-Repetitions"),
                fontSize,
                false,
                new Thickness(0, 16, 0, 0)
            );

            RelativePanel repetitionsDataRelativePanel = new RelativePanel { Margin = new Thickness(0, 10, 0, 0) };
            Grid.SetRow(repetitionsDataRelativePanel, row);
            Grid.SetColumn(repetitionsDataRelativePanel, 1);

            List<int> defaultRepetitionsValues = new List<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 15, 20, 25, 30, 40, 50, 100 };

            if (sound.DefaultRepetitions != int.MaxValue && !defaultRepetitionsValues.Contains(sound.DefaultRepetitions))
            {
                defaultRepetitionsValues.Add(sound.DefaultRepetitions);
                defaultRepetitionsValues.Sort();
            }

            RepetitionsComboBox = new ComboBox
            {
                IsEditable = true
            };
            RepetitionsComboBox.SelectionChanged += RepetitionsComboBox_SelectionChanged;
            RepetitionsComboBox.TextSubmitted += RepetitionsComboBox_TextSubmitted;

            foreach (int value in defaultRepetitionsValues)
                RepetitionsComboBox.Items.Add(value.ToString());

            RepetitionsComboBox.Items.Add("∞");

            if (sound.DefaultRepetitions == int.MaxValue)
                RepetitionsComboBox.SelectedValue = "∞";
            else
                RepetitionsComboBox.SelectedValue = sound.DefaultRepetitions.ToString();

            RelativePanel.SetAlignVerticalCenterWithPanel(RepetitionsComboBox, true);
            repetitionsDataRelativePanel.Children.Add(RepetitionsComboBox);

            row++;
            contentGrid.Children.Add(repetitionsHeaderStackPanel);
            contentGrid.Children.Add(repetitionsDataRelativePanel);
            #endregion

            #region Playback speed
            // Add the row
            var playbackSpeedRow = new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) };
            contentGrid.RowDefinitions.Add(playbackSpeedRow);

            StackPanel playbackSpeedHeaderStackPanel = GenerateTableCell(
                row,
                0,
                FileManager.loader.GetString("DefaultSoundSettingsDialog-PlaybackSpeed"),
                fontSize,
                false,
                new Thickness(0, 16, 0, 0)
            );

            RelativePanel playbackSpeedDataRelativePanel = new RelativePanel { Margin = new Thickness(0, 10, 0, 0) };
            Grid.SetRow(playbackSpeedDataRelativePanel, row);
            Grid.SetColumn(playbackSpeedDataRelativePanel, 1);

            // Create the ComboBox with the playback speed items
            PlaybackSpeedComboBox = new ComboBox();
            PlaybackSpeedComboBox.Items.Add("0.25×");
            PlaybackSpeedComboBox.Items.Add("0.5×");
            PlaybackSpeedComboBox.Items.Add("0.75×");
            PlaybackSpeedComboBox.Items.Add("1.0×");
            PlaybackSpeedComboBox.Items.Add("1.25×");
            PlaybackSpeedComboBox.Items.Add("1.5×");
            PlaybackSpeedComboBox.Items.Add("1.75×");
            PlaybackSpeedComboBox.Items.Add("2.0×");
            PlaybackSpeedComboBox.SelectionChanged += PlaybackSpeedComboBox_SelectionChanged;

            // Select the correct item
            switch (sound.DefaultPlaybackSpeed)
            {
                case 25:
                    PlaybackSpeedComboBox.SelectedIndex = 0;
                    break;
                case 50:
                    PlaybackSpeedComboBox.SelectedIndex = 1;
                    break;
                case 75:
                    PlaybackSpeedComboBox.SelectedIndex = 2;
                    break;
                case 125:
                    PlaybackSpeedComboBox.SelectedIndex = 4;
                    break;
                case 150:
                    PlaybackSpeedComboBox.SelectedIndex = 5;
                    break;
                case 175:
                    PlaybackSpeedComboBox.SelectedIndex = 6;
                    break;
                case 200:
                    PlaybackSpeedComboBox.SelectedIndex = 7;
                    break;
                default:
                    PlaybackSpeedComboBox.SelectedIndex = 3;
                    break;
            }

            RelativePanel.SetAlignVerticalCenterWithPanel(PlaybackSpeedComboBox, true);
            playbackSpeedDataRelativePanel.Children.Add(PlaybackSpeedComboBox);

            row++;
            contentGrid.Children.Add(playbackSpeedHeaderStackPanel);
            contentGrid.Children.Add(playbackSpeedDataRelativePanel);
            #endregion

            return contentGrid;
        }

        private void VolumeControl_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            volumeChanged = true;
        }

        private void VolumeControl_MuteChanged(object sender, bool e)
        {
            mutedChanged = true;
        }

        private void RepetitionsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            repetitionsChanged = true;
        }

        private void RepetitionsComboBox_TextSubmitted(ComboBox sender, ComboBoxTextSubmittedEventArgs args)
        {
            repetitionsChanged = true;

            if (args.Text == "∞") return;
            if (!int.TryParse(args.Text, out int value) || value < 0)
                RepetitionsComboBox.Text = "1";
        }

        private async void PlaybackSpeedComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int selectedPlaybackSpeed = 100;

            switch (PlaybackSpeedComboBox.SelectedIndex)
            {
                case 0:
                    selectedPlaybackSpeed = 25;
                    break;
                case 1:
                    selectedPlaybackSpeed = 50;
                    break;
                case 2:
                    selectedPlaybackSpeed = 75;
                    break;
                case 3:
                    selectedPlaybackSpeed = 100;
                    break;
                case 4:
                    selectedPlaybackSpeed = 125;
                    break;
                case 5:
                    selectedPlaybackSpeed = 150;
                    break;
                case 6:
                    selectedPlaybackSpeed = 175;
                    break;
                case 7:
                    selectedPlaybackSpeed = 200;
                    break;
            }

            sound.DefaultPlaybackSpeed = selectedPlaybackSpeed;
            await FileManager.SetDefaultPlaybackSpeedOfSoundAsync(sound.Uuid, selectedPlaybackSpeed);
        }

        private StackPanel GenerateTableCell(int row, int column, string text, int fontSize, bool isTextSelectionEnabled, Thickness? margin)
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

        private async void ContentDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            if (
                !volumeChanged
                && !mutedChanged
                && !repetitionsChanged
            ) return;

            if (volumeChanged || mutedChanged)
            {
                // Set the new values and update the DefaultVolume and DefaultMuted of all Sounds in all lists in ItemViewHolder
                sound.DefaultVolume = VolumeControl.Value;
                sound.DefaultMuted = VolumeControl.Muted;

                // Update the sound in the database
                await FileManager.SetDefaultVolumeOfSoundAsync(sound.Uuid, VolumeControl.Value, VolumeControl.Muted);
            }

            if (repetitionsChanged)
            {
                // Get the selected repetitions
                string defaultRepetitionsString = RepetitionsComboBox.Text;
                int defaultRepetitions = 0;

                if (defaultRepetitionsString == "∞")
                    defaultRepetitions = int.MaxValue;
                else
                    int.TryParse(defaultRepetitionsString, out defaultRepetitions);

                sound.DefaultRepetitions = defaultRepetitions;
                await FileManager.SetDefaultRepetitionsOfSoundAsync(sound.Uuid, defaultRepetitions);
            }

            await FileManager.ReloadSound(sound);
        }
    }
}
