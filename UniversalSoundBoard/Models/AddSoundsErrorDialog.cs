using System.Collections.Generic;
using UniversalSoundboard.DataAccess;

namespace UniversalSoundboard.Models
{
    public class AddSoundsErrorDialog : Dialog
    {
        public AddSoundsErrorDialog(List<string> soundsList)
            : base(
                  FileManager.loader.GetString("AddSoundsErrorDialog-Title"),
                  GetContentString(soundsList),
                  FileManager.loader.GetString("Actions-Close")
            ) { }

        private static string GetContentString(List<string> soundsList)
        {
            string soundNames = "";
            foreach (var name in soundsList)
                soundNames += $"\n- {name}";

            return string.Format(FileManager.loader.GetString("AddSoundsErrorDialog-Content"), soundNames);
        }
    }
}
