using davClassLibrary.Common;
using davClassLibrary.Models;
using UniversalSoundBoard;
using UniversalSoundBoard.DataAccess;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace UniversalSoundboard.Common
{
    public class TriggerAction : ITriggerAction
    {
        public void UpdateAllOfTable(int tableId)
        {
            UpdateView(tableId, false);
        }

        public void UpdateTableObject(TableObject tableObject, bool fileDownloaded)
        {
            UpdateView(tableObject.TableId, fileDownloaded);
        }

        public void DeleteTableObject(TableObject tableObject)
        {
            UpdateView(tableObject.TableId, false);
        }

        private void UpdateView(int tableId, bool fileDownloaded)
        {
            if (tableId == FileManager.ImageFileTableId || 
                (tableId == FileManager.SoundFileTableId && !fileDownloaded) ||
                tableId == FileManager.SoundTableId)
            {
                // Update the sounds
                CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                    (App.Current as App)._itemViewHolder.AllSoundsChanged = true;
                    FileManager.UpdateGridView();
                });
            }
            else if (tableId == FileManager.CategoryTableId)
            {
                // Update the categories
                CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                    FileManager.CreateCategoriesList();
                    (App.Current as App)._itemViewHolder.AllSoundsChanged = true;
                });
            }
            else if (tableId == FileManager.PlayingSoundTableId)
            {
                // Update the playing sounds
                CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                    FileManager.CreatePlayingSoundsList();
                });
            }
        }
    }
}
