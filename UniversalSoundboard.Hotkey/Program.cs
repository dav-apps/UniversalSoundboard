using System;
using System.Diagnostics;
using System.Windows.Forms;
using Windows.Storage;

namespace UniversalSoundboard.Hotkey
{
    class Program
    {
        private const string HotkeyCurrentProcessIdKey = "Hotkey.CurrentProcessId";

        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            KillOldProcess();

            Application.Run(new HotkeyAppContext());
        }

        private static void KillOldProcess()
        {
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey(HotkeyCurrentProcessIdKey))
            {
                int processId = (int)ApplicationData.Current.LocalSettings.Values[HotkeyCurrentProcessIdKey];
                try
                {
                    Process oldProcess = Process.GetProcessById(processId);
                    oldProcess.Kill();
                }
                catch (Exception) { }
            }

            ApplicationData.Current.LocalSettings.Values[HotkeyCurrentProcessIdKey] = Process.GetCurrentProcess().Id;
        }
    }
}
