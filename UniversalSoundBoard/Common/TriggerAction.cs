using davClassLibrary.Common;
using System;
using System.Diagnostics;
using UniversalSoundBoard;
using UniversalSoundBoard.DataAccess;

namespace UniversalSoundboard.Common
{
    public class TriggerAction : ITriggerAction
    {
        public void UpdateAll()
        {
            (App.Current as App)._itemViewHolder.AllSoundsChanged = true;
            FileManager.CreateCategoriesObservableCollection();
            FileManager.UpdateGridView();
        }

        public void UpdateTableObject(Guid uuid)
        {
            (App.Current as App)._itemViewHolder.AllSoundsChanged = true;
            FileManager.CreateCategoriesObservableCollection();
            FileManager.UpdateGridView();
        }
    }
}
