using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        ObservableCollection<Category> categories;
        List<StorageFile> items = new List<StorageFile>();
        CoreDispatcher dispatcher;
        CoreDispatcher currentDispatcher;

        public ShareTargetPage()
        {
            InitializeComponent();
            DataContextChanged += (s, e) => Bindings.Update();
            SetDarkThemeLayout();

            categories = new ObservableCollection<Category>();
            (App.Current as App)._itemViewHolder.CategoriesUpdated += ItemViewHolder_CategoriesUpdated;
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
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => await FileManager.CreateCategoriesListAsync());
        }

        private void SetDarkThemeLayout()
        {
            ContentRoot.Background = new SolidColorBrush(FileManager.GetApplicationThemeColor());
        }

        private void UpdateCategories()
        {
            // Get all Categories and show them
            categories.Clear();
            int i = -1;
            foreach (Category cat in (App.Current as App)._itemViewHolder.Categories)
                if (++i != 0)
                    categories.Add(cat);

            Bindings.Update();
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            AddButton.IsEnabled = false;
            AddCategoryButton.IsEnabled = false;
            LoadingControl.IsLoading = true;
            LoadingControlMessageTextBlock.Text = new Windows.ApplicationModel.Resources.ResourceLoader().GetString("AddSoundsMessage");

            List<Guid> categoryUuids = new List<Guid>();
            foreach(var item in CategoriesListView.SelectedItems)
            {
                var category = (Category)item;
                categoryUuids.Add(category.Uuid);
            }
            
            if (items.Count > 0)
            {
                foreach (StorageFile storagefile in items)
                {
                    if (FileManager.allowedFileTypes.Contains(storagefile.FileType))
                    {
                        await FileManager.AddSoundAsync(Guid.Empty, storagefile.DisplayName, categoryUuids, storagefile);
                    }
                }
                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    (App.Current as App)._itemViewHolder.AllSoundsChanged = true;
                });
            }

            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                await FileManager.UpdateGridViewAsync();
            });

            shareOperation.ReportCompleted();
        }
        
        private async void AddCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            // Show new Category ContentDialog
            var newCategoryContentDialog = ContentDialogs.CreateNewCategoryContentDialog();
            newCategoryContentDialog.PrimaryButtonClick += NewCategoryContentDialog_PrimaryButtonClick;
            await newCategoryContentDialog.ShowAsync();
        }

        private async void NewCategoryContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Get combobox value
            ComboBoxItem typeItem = (ComboBoxItem)ContentDialogs.IconSelectionComboBox.SelectedItem;
            string icon = typeItem.Content.ToString();

            Category category = new Category
            {
                Name = ContentDialogs.NewCategoryTextBox.Text,
                Icon = icon
            };
            
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => await FileManager.AddCategoryAsync(Guid.Empty, category.Name, category.Icon));
        }

        private async void ItemViewHolder_CategoriesUpdated(object sender, EventArgs e)
        {
            await currentDispatcher.RunAsync(CoreDispatcherPriority.Normal, UpdateCategories);
        }
    }
}
