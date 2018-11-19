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
using Windows.UI.Xaml.Navigation;

namespace UniversalSoundBoard.Pages
{
    public sealed partial class ShareTargetPage : Page
    {
        ShareOperation shareOperation;
        ObservableCollection<Category> categories;
        List<StorageFile> items = new List<StorageFile>();
        CoreDispatcher dispatcher;


        public ShareTargetPage()
        {
            InitializeComponent();
            DataContextChanged += (s, e) => Bindings.Update();
        }
        
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            dispatcher = MainPage.dispatcher == null ? CoreWindow.GetForCurrentThread().Dispatcher : MainPage.dispatcher;

            shareOperation = e.Parameter as ShareOperation;

            items.Clear();
            foreach (StorageFile file in await shareOperation.Data.GetStorageItemsAsync())
                items.Add(file);
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                categories = new ObservableCollection<Category>();
                FileManager.CreateCategoriesList();

                // Get all Categories and show them
                foreach (Category cat in (App.Current as App)._itemViewHolder.Categories)
                    categories.Add(cat);
            });
            Bindings.Update();

            (App.Current as App)._itemViewHolder.Categories.CollectionChanged += Categories_CollectionChanged;
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            AddButton.IsEnabled = false;
            AddCategoryButton.IsEnabled = false;
            LoadingControl.IsLoading = true;
            LoadingControlMessageTextBlock.Text = new Windows.ApplicationModel.Resources.ResourceLoader().GetString("AddSoundsMessage");

            Guid categoryUuid = Guid.Empty;
            if (CategoriesListView.SelectedIndex != 0)
                categoryUuid = (CategoriesListView.SelectedItem as Category).Uuid;
            
            if (items.Count > 0)
            {
                foreach (StorageFile storagefile in items)
                {
                    if (FileManager.allowedFileTypes.Contains(storagefile.FileType))
                    {
                        await FileManager.AddSound(Guid.Empty, storagefile.DisplayName, categoryUuid, storagefile);
                    }
                }
                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    (App.Current as App)._itemViewHolder.AllSoundsChanged = true;
                });
            }

            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                await FileManager.UpdateGridView();
            });
            
            (App.Current as App)._itemViewHolder.Categories.CollectionChanged -= Categories_CollectionChanged;

            shareOperation.ReportCompleted();
        }
        
        private void CategoriesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AddButton.IsEnabled = true;
        }

        private async void Categories_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                IEnumerable<Category> newCategoriesObservableCollection = (IEnumerable<Category>)sender;
                List<Category> newCategories = new List<Category>(newCategoriesObservableCollection);
                categories.Clear();
                foreach (var c in newCategories)
                    categories.Add(c);
            });
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
            
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                FileManager.AddCategory(Guid.Empty, category.Name, category.Icon);
            });
        }
    }
}
