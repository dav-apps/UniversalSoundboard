using davClassLibrary.Common;
using davClassLibrary.Models;
using System;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Pages;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace UniversalSoundboard.Common
{
    public class Callbacks : ICallbacks
    {
        public async void UpdateAllOfTable(int tableId, bool changed, bool complete)
        {
            if (!changed) return;
            CoreDispatcher dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;

            if (tableId == Constants.SoundTableId)
                await dispatcher.RunAsync(CoreDispatcherPriority.Low, async () =>
                {
                    if (FileManager.itemViewHolder.AppState == AppState.InitialSync)
                        FileManager.itemViewHolder.AppState = AppState.Loading;

                    FileManager.itemViewHolder.AllSoundsChanged = true;

                    if (FileManager.itemViewHolder.Page == typeof(SoundPage))
                    {
                        if (FileManager.itemViewHolder.SelectedCategory.Equals(Guid.Empty))
                            await FileManager.ShowAllSoundsAsync();
                        else
                            await FileManager.ShowCategoryAsync(FileManager.itemViewHolder.SelectedCategory);
                    }
                });
            else if (tableId == Constants.CategoryTableId && complete)
                await dispatcher.RunAsync(CoreDispatcherPriority.Low, async () => await FileManager.LoadCategoriesAsync());
            else if (tableId == Constants.PlayingSoundTableId && complete)
                await dispatcher.RunAsync(CoreDispatcherPriority.Low, async () => await FileManager.LoadPlayingSoundsAsync());
        }

        public async void UpdateTableObject(TableObject tableObject, bool fileDownloaded)
        {
            CoreDispatcher dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;

            if (tableObject.TableId == Constants.SoundTableId)
                await dispatcher.RunAsync(CoreDispatcherPriority.Low, async () => await FileManager.ReloadSound(tableObject.Uuid));
            else if (tableObject.TableId == Constants.CategoryTableId)
                await dispatcher.RunAsync(CoreDispatcherPriority.Low, async () => await FileManager.ReloadCategory(tableObject.Uuid));
            else if (tableObject.TableId == Constants.PlayingSoundTableId)
                await dispatcher.RunAsync(CoreDispatcherPriority.Low, async () => await FileManager.ReloadPlayingSoundAsync(tableObject.Uuid));
            else if (
                fileDownloaded
                && (
                    tableObject.TableId == Constants.SoundFileTableId
                    || tableObject.TableId == Constants.ImageFileTableId
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

            if (tableId == Constants.SoundTableId)
                await dispatcher.RunAsync(CoreDispatcherPriority.Low, () => FileManager.RemoveSound(uuid));
            else if (tableId == Constants.CategoryTableId)
                await dispatcher.RunAsync(CoreDispatcherPriority.Low, () => FileManager.RemoveCategory(uuid));
            else if (tableId == Constants.PlayingSoundTableId)
                await dispatcher.RunAsync(CoreDispatcherPriority.Low, () => FileManager.RemovePlayingSound(uuid));
        }

        public void TableObjectDownloadProgress(Guid uuid, int value)
        {
            FileManager.itemViewHolder.TriggerTableObjectFileDownloadProgressChangedEvent(
                this,
                new TableObjectFileDownloadProgressChangedEventArgs(
                    uuid,
                    value
                )
            );
        }

        public async void UserSyncFinished()
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                FileManager.itemViewHolder.TriggerUserSyncFinishedEvent(this, new EventArgs());
            });
        }

        public void SyncFinished()
        {
            FileManager.syncFinished = true;
            FileManager.DismissInAppNotification(InAppNotificationType.Sync);

            if (FileManager.itemViewHolder.AppState == AppState.InitialSync)
                FileManager.itemViewHolder.AppState = FileManager.itemViewHolder.AllSounds.Count > 0 ? AppState.Normal : AppState.Empty;
        }
    }
}
