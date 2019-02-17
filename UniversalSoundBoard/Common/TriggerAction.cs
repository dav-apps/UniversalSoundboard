using davClassLibrary.Common;
using davClassLibrary.Models;
using UniversalSoundBoard;
using UniversalSoundBoard.DataAccess;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;
using System;
using System.Threading.Tasks;

namespace UniversalSoundboard.Common
{
    public class TriggerAction : ITriggerAction
    {
        public void UpdateAllOfTable(int tableId)
        {
            UpdateView(tableId).Wait();
        }

        public void UpdateTableObject(TableObject tableObject, bool fileDownloaded)
        {
            if (tableObject.TableId == FileManager.PlayingSoundTableId)
            {
                CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    await FileManager.UpdatePlayingSoundListItem(tableObject.Uuid);
                }).AsTask().Wait();
            }
        }

        public void DeleteTableObject(TableObject tableObject)
        {
            UpdateView(tableObject.TableId).Wait();
        }

        public void SyncFinished()
        {
            FileManager.syncFinished = true;
        }

        private async Task UpdateView(int tableId)
        {
            if (tableId == FileManager.ImageFileTableId || tableId == FileManager.SoundFileTableId)
            {
                // Update the sounds
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    (App.Current as App)._itemViewHolder.AllSoundsChanged = true;
                    await FileManager.UpdateGridView();
                });
            }
            else if (tableId == FileManager.CategoryTableId)
            {
                // Update the categories
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    FileManager.CreateCategoriesList();
                    (App.Current as App)._itemViewHolder.AllSoundsChanged = true;
                });
            }
            else if (tableId == FileManager.PlayingSoundTableId)
            {
                // Update the playing sounds
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    await FileManager.CreatePlayingSoundsList();
                });
            }
        }
    }
}
