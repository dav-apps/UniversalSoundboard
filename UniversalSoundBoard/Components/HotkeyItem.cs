using System;
using UniversalSoundboard.Common;
using UniversalSoundboard.Models;

namespace UniversalSoundboard.Components
{
    public class HotkeyItem
    {
        public bool AddItem { get; }
        public Hotkey Hotkey { get; }
        public string Text { get; }
        public event EventHandler<HotkeyEventArgs> HotkeyAdded;
        public event EventHandler<HotkeyEventArgs> RemoveHotkey;

        public HotkeyItem()
        {
            AddItem = true;
            Hotkey = new Hotkey();
        }

        public HotkeyItem(Hotkey hotkey)
        {
            AddItem = false;
            Hotkey = hotkey;
            Text = hotkey.ToString();
        }

        public void TriggerHotkeyAddedEvent(object sender, Hotkey hotkey)
        {
            HotkeyAdded?.Invoke(sender, new HotkeyEventArgs(hotkey));
        }

        public void Remove()
        {
            RemoveHotkey?.Invoke(this, new HotkeyEventArgs(Hotkey));
        }
    }
}
