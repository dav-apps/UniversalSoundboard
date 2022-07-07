using System;
using System.Collections.Generic;
using UniversalSoundboard.Common;
using UniversalSoundboard.Components;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Models;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace UniversalSoundboard.Pages
{
    public sealed partial class ShareTargetPage : Page
    {
        ShareOperation shareOperation;
        List<StorageFile> items = new List<StorageFile>();
        CoreDispatcher dispatcher;
        CoreDispatcher currentDispatcher;

        public ShareTargetPage()
        {
            InitializeComponent();
            FileManager.itemViewHolder.PropertyChanged += ItemViewHolder_PropertyChanged;
            FileManager.itemViewHolder.CategoriesLoaded += ItemViewHolder_CategoriesLoaded;
            FileManager.itemViewHolder.CategoryAdded += ItemViewHolder_CategoryAdded;
            FileManager.itemViewHolder.CategoryUpdated += ItemViewHolder_CategoryUpdated;
            FileManager.itemViewHolder.CategoryDeleted += ItemViewHolder_CategoryDeleted;
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            dispatcher = MainPage.dispatcher ?? CoreWindow.GetForCurrentThread().Dispatcher;
            currentDispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
            shareOperation = e.Parameter as ShareOperation;

            // Get the shared files
            items.Clear();
            foreach (StorageFile file in await shareOperation.Data.GetStorageItemsAsync())
                items.Add(file);

            // Update the categories list
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => await FileManager.LoadCategoriesAsync());

            SetThemeColors();
        }

        private async void ItemViewHolder_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(ItemViewHolder.CurrentThemeKey))
                await currentDispatcher.RunAsync(CoreDispatcherPriority.Low, () => SetThemeColors());
        }

        private async void ItemViewHolder_CategoriesLoaded(object sender, EventArgs e)
        {
            await currentDispatcher.RunAsync(CoreDispatcherPriority.Normal, LoadCategories);
        }

        private async void ItemViewHolder_CategoryAdded(object sender, CategoryEventArgs e)
        {
            await currentDispatcher.RunAsync(CoreDispatcherPriority.Normal, LoadCategories);
        }

        private async void ItemViewHolder_CategoryUpdated(object sender, CategoryEventArgs e)
        {
            await currentDispatcher.RunAsync(CoreDispatcherPriority.Normal, LoadCategories);
        }

        private async void ItemViewHolder_CategoryDeleted(object sender, CategoryEventArgs e)
        {
            await currentDispatcher.RunAsync(CoreDispatcherPriority.Normal, LoadCategories);
        }

        private void SetThemeColors()
        {
            RequestedTheme = FileManager.GetRequestedTheme();
            ContentRoot.Background = new SolidColorBrush(FileManager.GetApplicationThemeColor());
        }

        private void LoadCategories()
        {
            // Get the categories
            List<Category> categories = new List<Category>();
            for (int i = 1; i < FileManager.itemViewHolder.Categories.Count; i++)
                categories.Add(FileManager.itemViewHolder.Categories[i]);

            // Create the nodes for the tree view
            CategoriesTreeView.RootNodes.Clear();

            foreach (var node in FileManager.CreateTreeViewNodesFromCategories(categories, new List<CustomTreeViewNode>(), new List<Guid>()))
                CategoriesTreeView.RootNodes.Add(node);
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            AddButton.IsEnabled = false;
            LoadingControl.IsLoading = true;
            LoadingControlMessageTextBlock.Text = new ResourceLoader().GetString("AddSoundsMessage");

            List<string> notAddedSounds = new List<string>();
            List<Guid> categoryUuids = new List<Guid>();
            foreach (CustomTreeViewNode node in CategoriesTreeView.SelectedNodes)
                categoryUuids.Add((Guid)node.Tag);
            
            if (items.Count > 0)
            {
                foreach (StorageFile storagefile in items)
                {
                    if (!FileManager.allowedFileTypes.Contains(storagefile.FileType)) continue;

                    Guid soundUuid = await FileManager.CreateSoundAsync(null, storagefile.DisplayName, categoryUuids, storagefile);

                    if (soundUuid.Equals(Guid.Empty))
                        notAddedSounds.Add(storagefile.Name);
                    else
                        await dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => await FileManager.AddSound(soundUuid));
                }
            }

            AddButton.IsEnabled = true;
            LoadingControl.IsLoading = false;

            if (notAddedSounds.Count > 0)
            {
                if (items.Count == 1)
                {
                    var addSoundErrorContentDialog = ContentDialogs.CreateAddSoundErrorContentDialog();
                    await ContentDialogs.ShowContentDialogAsync(addSoundErrorContentDialog);
                }
                else
                {
                    var addSoundsErrorDialog = new AddSoundsErrorDialog(notAddedSounds);
                    await addSoundsErrorDialog.ShowAsync();
                }
            }
            else
            {
                shareOperation.ReportCompleted();
            }
        }
    }
}
