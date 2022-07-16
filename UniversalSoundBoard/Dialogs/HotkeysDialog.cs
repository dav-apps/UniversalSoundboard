using System.Collections.Generic;
using UniversalSoundboard.Common;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Models;
using Windows.System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace UniversalSoundboard.Dialogs
{
    public class HotkeysDialog : Dialog
    {
        private TextBlock AddHotkeyButtonFlyoutTextBlock;
        private Button AddHotkeyButtonFlyoutAddButton;
        private readonly List<VirtualKey> CurrentlyPressedKeys = new List<VirtualKey>();
        private Hotkey PressedHotkey = new Hotkey();

        public HotkeysDialog(Sound sound)
            : base(
                  string.Format(FileManager.loader.GetString("HotkeysDialog-Title"), sound.Name),
                  FileManager.loader.GetString("Actions-Close")
            )
        {
            Content = GetContent(sound);
        }

        private RelativePanel GetContent(Sound sound)
        {
            RelativePanel contentRelativePanel = new RelativePanel();

            StackPanel textStackPanel = new StackPanel
            {
                Margin = new Thickness(0, 0, 0, 10)
            };
            RelativePanel.SetAlignTopWithPanel(textStackPanel, true);
            RelativePanel.SetAlignLeftWithPanel(textStackPanel, true);
            RelativePanel.SetAlignRightWithPanel(textStackPanel, true);

            TextBlock descriptionTextBlock = new TextBlock
            {
                Text = FileManager.loader.GetString("HotkeysDialog-Description"),
                TextWrapping = TextWrapping.WrapWholeWords
            };

            textStackPanel.Children.Add(descriptionTextBlock);

            ScrollViewer scrollViewer = new ScrollViewer();
            RelativePanel.SetBelow(scrollViewer, textStackPanel);

            StackPanel itemsStackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            // Add the add button
            Button addButton = new Button
            {
                Content = FileManager.loader.GetString("Actions-Add"),
                Margin = new Thickness(0, 4, 0, 4)
            };

            var addButtonFlyout = new Flyout();
            addButtonFlyout.Closed += AddButtonFlyout_Closed;

            StackPanel addButtonFlyoutStackPanel = new StackPanel
            {
                MinWidth = 200
            };
            addButtonFlyoutStackPanel.KeyDown += AddButtonFlyoutStackPanel_KeyDown;
            addButtonFlyoutStackPanel.KeyUp += AddButtonFlyoutStackPanel_KeyUp;

            AddHotkeyButtonFlyoutTextBlock = new TextBlock
            {
                Text = FileManager.loader.GetString("HotkeysDialog-FlyoutText")
            };

            StackPanel addHotkeyButtonFlyoutButtonStackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 15, 0, 0)
            };

            Button addHotkeyButtonFlyoutCancelButton = new Button
            {
                Content = FileManager.loader.GetString("Actions-Cancel"),
                Margin = new Thickness(0, 0, 10, 0)
            };
            addHotkeyButtonFlyoutCancelButton.Click += (object sender, RoutedEventArgs e) => addButtonFlyout.Hide();

            AddHotkeyButtonFlyoutAddButton = new Button
            {
                Content = FileManager.loader.GetString("Actions-Add"),
                IsEnabled = false
            };

            addHotkeyButtonFlyoutButtonStackPanel.Children.Add(addHotkeyButtonFlyoutCancelButton);
            addHotkeyButtonFlyoutButtonStackPanel.Children.Add(AddHotkeyButtonFlyoutAddButton);

            addButtonFlyoutStackPanel.Children.Add(AddHotkeyButtonFlyoutTextBlock);
            addButtonFlyoutStackPanel.Children.Add(addHotkeyButtonFlyoutButtonStackPanel);

            addButtonFlyout.Content = addButtonFlyoutStackPanel;
            addButton.Flyout = addButtonFlyout;

            itemsStackPanel.Children.Add(addButton);

            // Add an item for each hotkey
            foreach (var hotkey in sound.Hotkeys)
            {
                if (hotkey.IsEmpty())
                    continue;

                RelativePanel itemRelativePanel = new RelativePanel
                {
                    CornerRadius = new CornerRadius(2),
                    Margin = new Thickness(0, 4, 0, 4),
                    Padding = new Thickness(8, 4, 8, 4),
                    BorderBrush = new SolidColorBrush(),
                    BorderThickness = new Thickness(1, 0, 1, 1)
                };

                if (FileManager.itemViewHolder.CurrentTheme == AppTheme.Dark)
                {
                    itemRelativePanel.Background = new SolidColorBrush(Color.FromArgb(13, 255, 255, 255));
                    itemRelativePanel.BorderBrush = new SolidColorBrush(Color.FromArgb(25, 0, 0, 0));
                }
                else
                {
                    itemRelativePanel.Background = new SolidColorBrush(Color.FromArgb(15, 0, 0, 0));
                    itemRelativePanel.BorderBrush = new SolidColorBrush(Color.FromArgb(15, 0, 0, 0));
                }

                TextBlock hotkeyTextBlock = new TextBlock
                {
                    Text = hotkey.ToString(),
                    FontSize = 14,
                    Padding = new Thickness(0),
                    Margin = new Thickness(0)
                };

                Button closeButton = new Button
                {
                    FontFamily = new FontFamily(FileManager.FluentIconsFontFamily),
                    Content = "\uE894",
                    FontSize = 16,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Background = new SolidColorBrush(Colors.Transparent),
                    Margin = new Thickness(8, 0, 0, 0),
                    Padding = new Thickness(0),
                    BorderThickness = new Thickness(0)
                };

                RelativePanel.SetAlignVerticalCenterWithPanel(hotkeyTextBlock, true);
                RelativePanel.SetAlignVerticalCenterWithPanel(closeButton, true);
                RelativePanel.SetAlignLeftWithPanel(hotkeyTextBlock, true);
                RelativePanel.SetAlignRightWithPanel(closeButton, true);
                RelativePanel.SetRightOf(closeButton, hotkeyTextBlock);

                itemRelativePanel.Children.Add(hotkeyTextBlock);
                itemRelativePanel.Children.Add(closeButton);
                itemsStackPanel.Children.Add(itemRelativePanel);
            }

            scrollViewer.Content = itemsStackPanel;

            contentRelativePanel.Children.Add(textStackPanel);
            contentRelativePanel.Children.Add(scrollViewer);

            return contentRelativePanel;
        }

        private void AddButtonFlyout_Closed(object sender, object e)
        {
            // Clear the selected hotkey
            CurrentlyPressedKeys.Clear();
            AddHotkeyButtonFlyoutTextBlock.Text = FileManager.loader.GetString("HotkeysDialog-FlyoutText");
            AddHotkeyButtonFlyoutAddButton.IsEnabled = false;
        }

        private void AddButtonFlyoutStackPanel_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (CurrentlyPressedKeys.IndexOf(e.Key) == -1)
                CurrentlyPressedKeys.Add(e.Key);

            // Update the text
            PressedHotkey = FileManager.KeyListToHotkey(CurrentlyPressedKeys);

            if (PressedHotkey.IsEmpty())
                AddHotkeyButtonFlyoutTextBlock.Text = FileManager.loader.GetString("HotkeysDialog-FlyoutText");
            else
                AddHotkeyButtonFlyoutTextBlock.Text = PressedHotkey.ToString();

            AddHotkeyButtonFlyoutAddButton.IsEnabled = !PressedHotkey.IsEmpty();
        }

        private void AddButtonFlyoutStackPanel_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            int i = CurrentlyPressedKeys.IndexOf(e.Key);
            if (i != -1) CurrentlyPressedKeys.RemoveAt(i);
        }
    }
}
