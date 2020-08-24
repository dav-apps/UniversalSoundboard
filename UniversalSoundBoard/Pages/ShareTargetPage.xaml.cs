using System;
using System.Collections.Generic;
using UniversalSoundboard.Components;
using UniversalSoundBoard.Common;
using UniversalSoundBoard.DataAccess;
using UniversalSoundBoard.Models;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace UniversalSoundBoard.Pages
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
            FileManager.itemViewHolder.CategoriesUpdatedEvent += ItemViewHolder_CategoriesUpdated;
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
            if(e.PropertyName.Equals(ItemViewHolder.CurrentThemeKey))
                await currentDispatcher.RunAsync(CoreDispatcherPriority.Low, () => SetThemeColors());
        }

        private async void ItemViewHolder_CategoriesUpdated(object sender, EventArgs e)
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
            LoadingControlMessageTextBlock.Text = new Windows.ApplicationModel.Resources.ResourceLoader().GetString("AddSoundsMessage");

            List<Guid> categoryUuids = new List<Guid>();
            foreach (CustomTreeViewNode node in CategoriesTreeView.SelectedNodes)
                categoryUuids.Add((Guid)node.Tag);
            
            if (items.Count > 0)
            {
                foreach (StorageFile storagefile in items)
                {
                    if (!FileManager.allowedFileTypes.Contains(storagefile.FileType)) continue;

                    Guid soundUuid = await FileManager.CreateSoundAsync(null, storagefile.DisplayName, categoryUuids, storagefile);
                    await dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => await FileManager.AddSound(soundUuid));
                }
            }

            shareOperation.ReportCompleted();
        }
    }
}
