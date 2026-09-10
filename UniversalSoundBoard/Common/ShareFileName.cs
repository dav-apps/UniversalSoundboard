using System;
using System.Linq;

namespace UniversalSoundboard.Common
{
    public static class ShareFileName
    {
        public static string Create(string soundName, string extension)
        {
            // Sound titles are display text, not necessarily valid Windows file names.
            string name = Clean(soundName).Trim().TrimEnd('.');
            if (string.IsNullOrEmpty(name)) name = "Sound";
            string stem = name.Split('.')[0].TrimEnd();
            if (IsReserved(stem)) name = "_" + name;

            string ext = Clean(extension?.TrimStart('.')).Trim().TrimEnd('.');
            if (string.IsNullOrEmpty(ext)) ext = "mp3";
            if (ext.Length > 16) ext = ext.Substring(0, 16);
            // Leave room for the temporary folder path and collision suffixes.
            int maxNameLength = 120 - ext.Length - 1;
            if (name.Length > maxNameLength)
            {
                name = name.Substring(0, maxNameLength);
                if (char.IsHighSurrogate(name[name.Length - 1]))
                    name = name.Substring(0, name.Length - 1);
            }
            return name + "." + ext;
        }

        private static string Clean(string value) => new string((value ?? "")
            .Select(c => c < 32 || "<>:\"/\\|?*".IndexOf(c) >= 0 ? '_' : c).ToArray());

        private static bool IsReserved(string name)
        {
            name = name.ToUpperInvariant();
            return name == "CON" || name == "PRN" || name == "AUX" || name == "NUL"
                || (name.Length == 4 && (name.StartsWith("COM") || name.StartsWith("LPT"))
                    && "123456789¹²³".IndexOf(name[3]) >= 0);
        }
    }
}
