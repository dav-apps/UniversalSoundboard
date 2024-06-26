﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UniversalSoundboard.Common;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Models;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace UniversalSoundboard.Dialogs
{
    public class HotkeysDialog : Dialog
    {
        private Sound Sound;
        private Flyout AddButtonFlyout;
        private TextBlock AddHotkeyButtonFlyoutTextBlock;
        private Button AddHotkeyButtonFlyoutAddButton;
        private readonly List<VirtualKey> CurrentlyPressedKeys = new List<VirtualKey>();
        private Hotkey PressedHotkey = new Hotkey();
        ObservableCollection<HotkeyItem> HotkeyItems = new ObservableCollection<HotkeyItem>();

        public HotkeysDialog(Sound sound, DataTemplate hotkeyItemTemplate, Style accentButtonStyle)
            : base(
                  string.Format(FileManager.loader.GetString("HotkeysDialog-Title"), sound.Name),
                  FileManager.loader.GetString("Actions-Close")
            )
        {
            Sound = sound;

            foreach (var hotkey in sound.Hotkeys)
            {
                if (hotkey.IsEmpty())
                    continue;

                var hotkeyItem = new HotkeyItem(hotkey);
                hotkeyItem.RemoveHotkey += HotkeyItem_RemoveHotkey;

                HotkeyItems.Add(hotkeyItem);
            }

            Content = GetContent(hotkeyItemTemplate, accentButtonStyle);
        }

        private StackPanel GetContent(DataTemplate hotkeyItemTemplate, Style accentButtonStyle)
        {
            StackPanel contentStackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical
            };

            // Add description
            TextBlock descriptionTextBlock = new TextBlock
            {
                Margin = new Thickness(0, 0, 0, 10),
                Text = FileManager.loader.GetString("HotkeysDialog-Description"),
                TextWrapping = TextWrapping.WrapWholeWords
            };

            contentStackPanel.Children.Add(descriptionTextBlock);

            // Add the add button
            StackPanel addButtonContentStackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal
            };

            TextBlock addButtonIconTextBlock = new TextBlock
            {
                Text = "\uE109",
                FontFamily = new Windows.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"),
                FontSize = 16,
                VerticalAlignment = VerticalAlignment.Center
            };

            TextBlock addButtonContentTextBlock = new TextBlock
            {
                Text = FileManager.loader.GetString("Actions-Add"),
                Margin = new Thickness(12, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            addButtonContentStackPanel.Children.Add(addButtonIconTextBlock);
            addButtonContentStackPanel.Children.Add(addButtonContentTextBlock);

            Button addButton = new Button
            {
                Content = addButtonContentStackPanel,
                Margin = new Thickness(0, 4, 0, 6),
                Style = accentButtonStyle
            };

            AddButtonFlyout = new Flyout();
            AddButtonFlyout.Closed += AddButtonFlyout_Closed;

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

            AddHotkeyButtonFlyoutAddButton = new Button
            {
                Content = FileManager.loader.GetString("Actions-Add"),
                IsEnabled = false,
                Margin = new Thickness(0, 0, 10, 0)
            };
            AddHotkeyButtonFlyoutAddButton.Click += AddHotkeyButtonFlyoutAddButton_Click;

            Button addHotkeyButtonFlyoutCancelButton = new Button
            {
                Content = FileManager.loader.GetString("Actions-Cancel")
            };
            addHotkeyButtonFlyoutCancelButton.Click += (object sender, RoutedEventArgs e) => AddButtonFlyout.Hide();

            addHotkeyButtonFlyoutButtonStackPanel.Children.Add(AddHotkeyButtonFlyoutAddButton);
            addHotkeyButtonFlyoutButtonStackPanel.Children.Add(addHotkeyButtonFlyoutCancelButton);

            addButtonFlyoutStackPanel.Children.Add(AddHotkeyButtonFlyoutTextBlock);
            addButtonFlyoutStackPanel.Children.Add(addHotkeyButtonFlyoutButtonStackPanel);

            AddButtonFlyout.Content = addButtonFlyoutStackPanel;
            addButton.Flyout = AddButtonFlyout;

            ListView hotkeyListView = new ListView
            {
                ItemTemplate = hotkeyItemTemplate,
                ItemsSource = HotkeyItems,
                SelectionMode = ListViewSelectionMode.None,
                Margin = new Thickness(-16, 0, 0, 0),
                MaxHeight = 120
            };

            contentStackPanel.Children.Add(addButton);
            contentStackPanel.Children.Add(hotkeyListView);

            return contentStackPanel;
        }

        private async void AddHotkeyButtonFlyoutAddButton_Click(object sender, RoutedEventArgs e)
        {
            AddButtonFlyout.Hide();

            // Add the hotkey to the list
            var hotkeyItem = new HotkeyItem(PressedHotkey);
            hotkeyItem.RemoveHotkey += HotkeyItem_RemoveHotkey;
            HotkeyItems.Insert(0, hotkeyItem);

            // Add the hotkey to the hotkeys of the sound
            Sound.Hotkeys.Insert(0, PressedHotkey);

            // Save the hotkeys of the sound
            await FileManager.SetHotkeysOfSoundAsync(Sound.Uuid, Sound.Hotkeys);

            // Update the Hotkey process with the new hotkeys
            await FileManager.StartHotkeyProcess();
        }

        private async void HotkeyItem_RemoveHotkey(object sender, HotkeyEventArgs e)
        {
            // Remove the hotkey item from the list
            HotkeyItems.Remove(sender as HotkeyItem);

            // Remove the hotkey from the list of hotkeys
            int i = Sound.Hotkeys.FindIndex(h => h.Modifiers == e.Hotkey.Modifiers && h.Key == e.Hotkey.Key);
            if (i != -1) Sound.Hotkeys.RemoveAt(i);

            // Save the hotkeys of the sound
            await FileManager.SetHotkeysOfSoundAsync(Sound.Uuid, Sound.Hotkeys);

            // Update the Hotkey process with the new hotkeys
            await FileManager.StartHotkeyProcess();
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
