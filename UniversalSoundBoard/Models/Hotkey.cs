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
            return Modifiers == Modifiers.None && Key == VirtualKey.None;
        }

        public override string ToString()
        {
            if (Modifiers == Modifiers.None && Key == VirtualKey.None)
                return "";

            if (Modifiers == Modifiers.None)
                return Key.ToString();

            string text = "";

            // TODO: Add translations
            switch (Modifiers)
            {
                case Modifiers.Alt:
                    text = VirtualKeyModifiers.Menu.ToString();
                    break;
                case Modifiers.Control:
                    text = VirtualKeyModifiers.Control.ToString();
                    break;
                case Modifiers.AltControl:
                    text = $"{VirtualKeyModifiers.Control} + {VirtualKeyModifiers.Menu}";
                    break;
                case Modifiers.Shift:
                    text = VirtualKeyModifiers.Shift.ToString();
                    break;
                case Modifiers.AltShift:
                    text = $"{VirtualKeyModifiers.Menu} + {VirtualKeyModifiers.Shift}";
                    break;
                case Modifiers.ControlShift:
                    text = $"{VirtualKeyModifiers.Control} + {VirtualKeyModifiers.Shift}";
                    break;
                case Modifiers.AltControlShift:
                    text = $"{VirtualKeyModifiers.Control} + {VirtualKeyModifiers.Menu} + {VirtualKeyModifiers.Shift}";
                    break;
                case Modifiers.AltWindows:
                    text = $"{VirtualKeyModifiers.Menu} + {VirtualKeyModifiers.Windows}";
                    break;
                case Modifiers.ControlWindows:
                    text = $"{VirtualKeyModifiers.Control} + {VirtualKeyModifiers.Windows}";
                    break;
                case Modifiers.AltControlWindows:
                    text = $"{VirtualKeyModifiers.Control} + {VirtualKeyModifiers.Menu} + {VirtualKeyModifiers.Windows}";
                    break;
                case Modifiers.ShiftWindows:
                    text = $"{VirtualKeyModifiers.Shift} + {VirtualKeyModifiers.Windows}";
                    break;
                case Modifiers.AltShiftWindows:
                    text = $"{VirtualKeyModifiers.Menu} + {VirtualKeyModifiers.Shift} + {VirtualKeyModifiers.Windows}";
                    break;
                case Modifiers.ControlShiftWindows:
                    text = $"{VirtualKeyModifiers.Control} + {VirtualKeyModifiers.Shift} + {VirtualKeyModifiers.Windows}";
                    break;
            }

            if (Key != VirtualKey.None)
                text += $" + {Key}";

            return text;
        }

        public string ToDataString()
        {
            // Return the hotkey in the format "x:y", with x for the modifiers value and y for the key value
            return $"{(int)Modifiers}:{(int)Key}";
        }
    }
}
