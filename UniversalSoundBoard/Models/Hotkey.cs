using Windows.System;
using static UniversalSoundboard.DataAccess.FileManager;

namespace UniversalSoundboard.Models
{
    public class Hotkey
    {
        public Modifiers Modifiers { get; set; }
        public VirtualKey Key { get; set; }

        public Hotkey()
        {
            Modifiers = Modifiers.None;
            Key = VirtualKey.None;
        }

        public bool IsEmpty()
        {
            // Special cases for invalid key combinations
            if (Modifiers == Modifiers.Windows) return true;

            return Modifiers == Modifiers.None && Key == VirtualKey.None;
        }

        public override string ToString()
        {
            if (Modifiers == Modifiers.None && Key == VirtualKey.None)
                return "";

            if (Modifiers == Modifiers.None)
                return VirtualKeyToString(Key);

            string text = "";

            switch (Modifiers)
            {
                case Modifiers.Alt:
                    text = VirtualKeyModifiersToString(VirtualKeyModifiers.Menu);
                    break;
                case Modifiers.Control:
                    text = VirtualKeyModifiersToString(VirtualKeyModifiers.Control);
                    break;
                case Modifiers.AltControl:
                    text = $"{VirtualKeyModifiersToString(VirtualKeyModifiers.Control)} + {VirtualKeyModifiersToString(VirtualKeyModifiers.Menu)}";
                    break;
                case Modifiers.Shift:
                    text = VirtualKeyModifiersToString(VirtualKeyModifiers.Shift);
                    break;
                case Modifiers.AltShift:
                    text = $"{VirtualKeyModifiersToString(VirtualKeyModifiers.Menu)} + {VirtualKeyModifiersToString(VirtualKeyModifiers.Shift)}";
                    break;
                case Modifiers.ControlShift:
                    text = $"{VirtualKeyModifiersToString(VirtualKeyModifiers.Control)} + {VirtualKeyModifiersToString(VirtualKeyModifiers.Shift)}";
                    break;
                case Modifiers.AltControlShift:
                    text = $"{VirtualKeyModifiersToString(VirtualKeyModifiers.Control)} + {VirtualKeyModifiersToString(VirtualKeyModifiers.Menu)} + {VirtualKeyModifiersToString(VirtualKeyModifiers.Shift)}";
                    break;
                case Modifiers.AltWindows:
                    text = $"{VirtualKeyModifiersToString(VirtualKeyModifiers.Menu)} + {VirtualKeyModifiersToString(VirtualKeyModifiers.Windows)}";
                    break;
                case Modifiers.ControlWindows:
                    text = $"{VirtualKeyModifiersToString(VirtualKeyModifiers.Control)} + {VirtualKeyModifiersToString(VirtualKeyModifiers.Windows)}";
                    break;
                case Modifiers.AltControlWindows:
                    text = $"{VirtualKeyModifiersToString(VirtualKeyModifiers.Control)} + {VirtualKeyModifiersToString(VirtualKeyModifiers.Menu)} + {VirtualKeyModifiersToString(VirtualKeyModifiers.Windows)}";
                    break;
                case Modifiers.ShiftWindows:
                    text = $"{VirtualKeyModifiersToString(VirtualKeyModifiers.Shift)} + {VirtualKeyModifiersToString(VirtualKeyModifiers.Windows)}";
                    break;
                case Modifiers.AltShiftWindows:
                    text = $"{VirtualKeyModifiersToString(VirtualKeyModifiers.Menu)} + {VirtualKeyModifiersToString(VirtualKeyModifiers.Shift)} + {VirtualKeyModifiersToString(VirtualKeyModifiers.Windows)}";
                    break;
                case Modifiers.ControlShiftWindows:
                    text = $"{VirtualKeyModifiersToString(VirtualKeyModifiers.Control)} + {VirtualKeyModifiersToString(VirtualKeyModifiers.Shift)} + {VirtualKeyModifiersToString(VirtualKeyModifiers.Windows)}";
                    break;
            }

            if (Key != VirtualKey.None)
                text += $" + {VirtualKeyToString(Key)}";

            return text;
        }

        public string ToDataString()
        {
            // Return the hotkey in the format "x:y", with x for the modifiers value and y for the key value
            return $"{(int)Modifiers}:{(int)Key}";
        }
    }
}
