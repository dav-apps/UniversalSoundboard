using System.Collections.ObjectModel;
using UniversalSoundboard.Common;
using UniversalSoundboard.Models;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Components
{
    public sealed partial class SettingsHotkeysSoundItem : UserControl
    {
        public Sound Sound;
        public string name = "";
        ObservableCollection<HotkeyItem> HotkeyItems = new ObservableCollection<HotkeyItem>();

        public SettingsHotkeysSoundItem()
        {
            InitializeComponent();
            DataContextChanged += SettingsHotkeysSoundItem_DataContextChanged;
        }

        private void SettingsHotkeysSoundItem_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (DataContext == null) return;

            Sound = (Sound)DataContext;
            name = Sound.Name;

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

        private void HotkeyItem_RemoveHotkey(object sender, HotkeyEventArgs e)
        {
            
        }
    }
}
