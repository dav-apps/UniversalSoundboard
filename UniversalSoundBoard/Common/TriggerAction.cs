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
            
        }

        public void UpdateAllOfTable(int tableId)
        {
            if (tableId == FileManager.SoundFileTableId ||
                tableId == FileManager.ImageFileTableId ||
                tableId == FileManager.SoundTableId)
            {
                // Update the sounds
                FileManager.UpdateGridView();
            } else if (tableId == FileManager.CategoryTableId)
            {
                // Update the categories
                FileManager.CreateCategoriesList();
            }else if(tableId == FileManager.PlayingSoundTableId)
            {
                // Update the playing sounds
                FileManager.CreatePlayingSoundsList();
            }
        }

        public void UpdateTableObject(Guid uuid)
        {
            (App.Current as App)._itemViewHolder.AllSoundsChanged = true;
            FileManager.CreateCategoriesList();
            FileManager.CreatePlayingSoundsList();
            FileManager.UpdateGridView();
        }
    }
}
