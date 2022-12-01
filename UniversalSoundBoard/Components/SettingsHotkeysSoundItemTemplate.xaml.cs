using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using UniversalSoundboard.Common;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Models;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace UniversalSoundboard.Components
{
    public sealed partial class SettingsHotkeysSoundItemTemplate : UserControl
    {
        public Sound Sound;
        public string name = "";
        private SolidColorBrush background = new SolidColorBrush();
        ObservableCollection<HotkeyItem> HotkeyItems = new ObservableCollection<HotkeyItem>();

        public event EventHandler<SoundEventArgs> Remove;

        public SettingsHotkeysSoundItemTemplate()
        {
            InitializeComponent();
            DataContextChanged += SettingsHotkeysSoundItem_DataContextChanged;
            FileManager.itemViewHolder.PropertyChanged += ItemViewHolder_PropertyChanged;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            SetThemeColors();
        }

        private void SettingsHotkeysSoundItem_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (DataContext == null) return;

            Sound = (Sound)DataContext;
            name = Sound.Name;

            HotkeyItems.Clear();

            foreach (var hotkey in Sound.Hotkeys)
            {
                if (hotkey.IsEmpty())
                    continue;

                var hotkeyItem = new HotkeyItem(hotkey);
                hotkeyItem.RemoveHotkey += HotkeyItem_RemoveHotkey;

                HotkeyItems.Add(hotkeyItem);
            }

            Bindings.Update();
        }

        private void ItemViewHolder_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(ItemViewHolder.CurrentThemeKey))
                SetThemeColors();
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

            // Remove the sound, if the last hotkey was removed
            if (HotkeyItems.Count == 0)
                Remove?.Invoke(this, new SoundEventArgs(Sound.Uuid));
        }

        private void SetThemeColors()
        {
            if (FileManager.itemViewHolder.CurrentTheme == AppTheme.Dark)
                background = new SolidColorBrush(Color.FromArgb(255, 34, 39, 52));
            else
                background = new SolidColorBrush(Color.FromArgb(255, 245, 245, 245));

            Bindings.Update();
        }
    }
}
