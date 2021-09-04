using System;
using UniversalSoundboard.Common;
using UniversalSoundboard.Models;

namespace UniversalSoundboard.Components
{
    public class HotkeyItem
    {
        public bool AddItem { get; set; }
        public string Text { get; set; }
        public event EventHandler<HotkeyEventArgs> HotkeyAdded;

        public HotkeyItem()
        {
            AddItem = true;
        }

        public HotkeyItem(string text)
        {
            AddItem = false;
            Text = text;
        }

        public void TriggerHotkeyAddedEvent(object sender, Hotkey hotkey)
        {
            HotkeyAdded?.Invoke(sender, new HotkeyEventArgs(hotkey));
        }
    }
}
