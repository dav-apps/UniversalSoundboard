using System.Collections.Generic;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Models;
using Windows.ApplicationModel.Resources;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;

namespace UniversalSoundboard.Components
{
    public sealed partial class HotkeyItemTemplate : UserControl
    {
        HotkeyItem HotkeyItem { get; set; }

        readonly ResourceLoader loader = new ResourceLoader();
        readonly List<VirtualKey> PropertiesDialogCurrentlyPressedKeys = new List<VirtualKey>();
        Hotkey PropertiesDialogPressedHotkey = new Hotkey();

        public HotkeyItemTemplate()
        {
            InitializeComponent();
            DataContextChanged += HotkeyItemTemplate_DataContextChanged;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            AddHotkeyButtonFlyoutText.Text = loader.GetString("PropertiesContentDialog-HotkeyFlyoutText");
        }

        private void HotkeyItemTemplate_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (DataContext == null) return;

            HotkeyItem = DataContext as HotkeyItem;
        }

        private void AddHotkeyButtonFlyoutStackPanel_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (PropertiesDialogCurrentlyPressedKeys.IndexOf(e.Key) == -1)
                PropertiesDialogCurrentlyPressedKeys.Add(e.Key);

            // Update the text
            PropertiesDialogPressedHotkey = FileManager.KeyListToHotkey(PropertiesDialogCurrentlyPressedKeys);

            if (PropertiesDialogPressedHotkey.IsEmpty())
                AddHotkeyButtonFlyoutText.Text = loader.GetString("PropertiesContentDialog-HotkeyFlyoutText");
            else
                AddHotkeyButtonFlyoutText.Text = PropertiesDialogPressedHotkey.ToString();

            AddButton.IsEnabled = !PropertiesDialogPressedHotkey.IsEmpty();
        }

        private void AddHotkeyButtonFlyoutStackPanel_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            int i = PropertiesDialogCurrentlyPressedKeys.IndexOf(e.Key);
            if (i != -1)
                PropertiesDialogCurrentlyPressedKeys.RemoveAt(i);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            AddHotkeyButton.Flyout.Hide();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (PropertiesDialogPressedHotkey.IsEmpty()) return;

            HotkeyItem.TriggerHotkeyAddedEvent(this, PropertiesDialogPressedHotkey);

            // Hide and reset the flyout
            AddHotkeyButton.Flyout.Hide();
            AddHotkeyButtonFlyoutText.Text = loader.GetString("PropertiesContentDialog-HotkeyFlyoutText");
            AddButton.IsEnabled = false;
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            HotkeyButton.Flyout.Hide();
            HotkeyItem.Remove();
        }
    }
}
