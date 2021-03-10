using davClassLibrary.Common;
using davClassLibrary.Models;
using System;
using UniversalSoundboard.DataAccess;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace UniversalSoundboard.Common
{
    public class Callbacks : ICallbacks
    {
        public async void UpdateAllOfTable(int tableId, bool changed)
        {
            CoreDispatcher dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;

            if (tableId == FileManager.SoundTableId)
                await dispatcher.RunAsync(CoreDispatcherPriority.Low, async () => await FileManager.AddAllSounds());
            else if (tableId == FileManager.CategoryTableId)
                await dispatcher.RunAsync(CoreDispatcherPriority.Low, async () => await FileManager.LoadCategoriesAsync());
            else if (tableId == FileManager.PlayingSoundTableId)
                await dispatcher.RunAsync(CoreDispatcherPriority.Low, async () => await FileManager.LoadPlayingSoundsAsync());
        }

        public async void UpdateTableObject(TableObject tableObject, bool fileDownloaded)
        {
            CoreDispatcher dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;

            if (tableObject.TableId == FileManager.SoundTableId)
                await dispatcher.RunAsync(CoreDispatcherPriority.Low, async () => await FileManager.ReloadSound(tableObject.Uuid));
            else if(tableObject.TableId == FileManager.CategoryTableId)
                await dispatcher.RunAsync(CoreDispatcherPriority.Low, async () => await FileManager.ReloadCategory(tableObject.Uuid));
            else if(tableObject.TableId == FileManager.PlayingSoundTableId)
                await dispatcher.RunAsync(CoreDispatcherPriority.Low, async () => await FileManager.ReloadPlayingSoundAsync(tableObject.Uuid));
            else if(
                fileDownloaded
                && (
                    tableObject.TableId == FileManager.SoundFileTableId
                    || tableObject.TableId == FileManager.ImageFileTableId
                )
            )
            {
                FileManager.itemViewHolder.TriggerTableObjectFileDownloadCompletedEvent(
                    this,
                    new TableObjectFileDownloadCompletedEventArgs(
                        tableObject.Uuid,
                        tableObject.File
                    )
                );
            }
        }

        public async void DeleteTableObject(Guid uuid, int tableId)
        {
            CoreDispatcher dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;

            if (tableId == FileManager.SoundTableId)
                await dispatcher.RunAsync(CoreDispatcherPriority.Low, () => FileManager.RemoveSound(uuid));
            else if (tableId == FileManager.CategoryTableId)
                await dispatcher.RunAsync(CoreDispatcherPriority.Low, () => FileManager.RemoveCategory(uuid));
            else if (tableId == FileManager.PlayingSoundTableId)
                await dispatcher.RunAsync(CoreDispatcherPriority.Low, () => FileManager.RemovePlayingSound(uuid));
        }

        public void TableObjectDownloadProgress(TableObject tableObject, int value)
        {
            FileManager.itemViewHolder.TriggerTableObjectFileDownloadProgressChangedEvent(
                this,
                new TableObjectFileDownloadProgressChangedEventArgs(
                    tableObject.Uuid,
                    value
                )
            );
        }

        public void UserSyncFinished()
        {
            FileManager.itemViewHolder.TriggerUserSyncFinished(this, new EventArgs());
        }

        public void SyncFinished()
        {
            FileManager.syncFinished = true;

            if (FileManager.itemViewHolder.AppState == FileManager.AppState.InitialSync)
                FileManager.itemViewHolder.AppState = FileManager.AppState.Normal;
        }
    }
}
