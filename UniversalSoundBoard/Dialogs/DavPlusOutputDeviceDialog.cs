﻿using UniversalSoundboard.DataAccess;

namespace UniversalSoundboard.Dialogs
{
    public class DavPlusOutputDeviceDialog : Dialog
    {
        public DavPlusOutputDeviceDialog()
            : base(
                  FileManager.loader.GetString("DavPlusDialog-Title"),
                  FileManager.loader.GetString("DavPlusOutputDeviceDialog-Content"),
                  FileManager.loader.GetString("Actions-LearnMore"),
                  FileManager.loader.GetString("Actions-Close")
            ) { }
    }
}
