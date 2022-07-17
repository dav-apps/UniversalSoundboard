using System;
using UniversalSoundboard.Common;
using UniversalSoundboard.Models;

namespace UniversalSoundboard.Components
{
    public class HotkeyItem
    {
        public Hotkey Hotkey { get; private set; }
        public string Text { get => Hotkey.ToString(); }

        public event EventHandler<HotkeyEventArgs> RemoveHotkey;

        public HotkeyItem(Hotkey hotkey)
        {
            Hotkey = hotkey;
        }

        public void Remove()
        {
            RemoveHotkey?.Invoke(this, new HotkeyEventArgs(Hotkey));
        }
    }
}
