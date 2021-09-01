using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Windows.Storage;
using Windows.ApplicationModel;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;

namespace UniversalSoundboard.Hotkey
{
    class HotkeyAppContext : ApplicationContext
    {
        private const string ProcessIdKey = "ProcessId";
        private const string HotkeysKey = "Hotkeys";

        private HotkeyWindow hotkeyWindow = null;
        private Process process = null;
        private bool hotkeyInProgress = false;

        public HotkeyAppContext()
        {
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(ProcessIdKey))
            {
                Application.Exit();
                return;
            }

            int processId = (int)ApplicationData.Current.LocalSettings.Values[ProcessIdKey];
            process = Process.GetProcessById(processId);
            process.EnableRaisingEvents = true;
            process.Exited += HotkeyAppContext_Exited;
            
            RegisterHotkeys();
        }

        private void RegisterHotkeys()
        {
            hotkeyWindow = new HotkeyWindow();
            hotkeyWindow.HotkeyPressed += new HotkeyWindow.HotkeyDelegate(hotkeys_HotkeyPressed);

            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(HotkeysKey))
                return;

            string hotkeysString = (string)ApplicationData.Current.LocalSettings.Values[HotkeysKey];
            if (string.IsNullOrEmpty(hotkeysString)) return;

            int i = 0;

            foreach (string hotkey in hotkeysString.Split(','))
            {
                string[] hotkeyValues = hotkey.Split(':');

                int modifiers = 0;
                int.TryParse(hotkeyValues[0], out modifiers);

                int key = 0;
                int.TryParse(hotkeyValues[1], out key);

                if (modifiers == 0 && key == 0)
                    continue;

                hotkeyWindow.RegisterCombo(i, modifiers, key);
                i++;
            }
        }

        private void HotkeyAppContext_Exited(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private async void hotkeys_HotkeyPressed(int id)
        {
            if (hotkeyInProgress) return;
            hotkeyInProgress = true;

            // send the key ID to the UWP
            ValueSet hotkeyPressed = new ValueSet { { "id", id } };

            AppServiceConnection connection = new AppServiceConnection
            {
                PackageFamilyName = Package.Current.Id.FamilyName,
                AppServiceName = "HotkeyConnection"
            };

            AppServiceConnectionStatus status = await connection.OpenAsync();
            if (status != AppServiceConnectionStatus.Success)
            {
                Application.Exit();
                return;
            }
            connection.ServiceClosed += Connection_ServiceClosed;
            AppServiceResponse response = await connection.SendMessageAsync(hotkeyPressed);
        }

        private void Connection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            hotkeyInProgress = false;
        }
    }

    public class HotkeyWindow : NativeWindow
    {
        private const int WM_HOTKEY = 0x0312;
        private const int WM_DESTROY = 0x0002;

        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vlc);
        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private List<int> ids = new List<int>();
        public delegate void HotkeyDelegate(int ID);
        public event HotkeyDelegate HotkeyPressed;

        // Creates a headless Window to register for and handle WM_HOTKEY
        public HotkeyWindow()
        {
            CreateHandle(new CreateParams());
            Application.ApplicationExit += new EventHandler(Application_ApplicationExit);
        }

        public void RegisterCombo(int id, int modifiers, int key)
        {
            if (RegisterHotKey(Handle, id, modifiers, key))
                ids.Add(id);
        }

        private void Application_ApplicationExit(object sender, EventArgs e)
        {
            DestroyHandle();
        }

        protected override void WndProc(ref Message m)
        {
            switch (m.Msg)
            {
                case WM_HOTKEY: // Raise the HotkeyPressed event
                    HotkeyPressed?.Invoke(m.WParam.ToInt32());
                    break;
                case WM_DESTROY: // Unregister all hotkeys
                    foreach (int id in ids)
                        UnregisterHotKey(Handle, id);
                    break;
            }

            base.WndProc(ref m);
        }
    }
}
