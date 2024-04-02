using System;
using UniversalSoundboard.DataAccess;

namespace UniversalSoundboard.Dialogs
{
    public class UpgradePlusDialog : Dialog
    {
        public event EventHandler<EventArgs> UpgradePlusSucceeded;

        public UpgradePlusDialog()
            : base(
                  new UpgradePlusContentDialog(),
                  FileManager.loader.GetString("Actions-Close")
            )
        {
            var upgradePlusContentDialog = ContentDialog as UpgradePlusContentDialog;
            upgradePlusContentDialog.UpgradePlusSucceeded += UpgradePlusContentDialog_UpgradePlusSucceeded;
        }

        private void UpgradePlusContentDialog_UpgradePlusSucceeded(object sender, EventArgs e)
        {
            UpgradePlusSucceeded?.Invoke(this, e);
            Hide();
        }
    }
}
