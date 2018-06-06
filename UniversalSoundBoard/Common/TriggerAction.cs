using davClassLibrary.Common;
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
            UpdateView(tableId, false);
        }

        public void UpdateTableObject(davClassLibrary.Models.TableObject tableObject, bool fileDownloaded)
        {
            UpdateView(tableObject.TableId, fileDownloaded);
        }

        private void UpdateView(int tableId, bool fileDownloaded)
        {
            if (tableId == FileManager.ImageFileTableId || 
                (tableId == FileManager.SoundFileTableId && !fileDownloaded) ||
                tableId == FileManager.SoundTableId)
            {
                // Update the sounds
                (App.Current as App)._itemViewHolder.AllSoundsChanged = true;
                FileManager.UpdateGridView();
            }
            else if (tableId == FileManager.CategoryTableId)
            {
                // Update the categories
                FileManager.CreateCategoriesList();
                (App.Current as App)._itemViewHolder.AllSoundsChanged = true;
            }
            else if (tableId == FileManager.PlayingSoundTableId)
            {
                // Update the playing sounds
                FileManager.CreatePlayingSoundsList();
            }
        }
    }
}
