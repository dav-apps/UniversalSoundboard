using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
        List<StorageFile> items;
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
            items = new List<StorageFile>();

            foreach (StorageFile file in await shareOperation.Data.GetStorageItemsAsync())
            {
                items.Add(file);
            }
        }
        
        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                categories = new ObservableCollection<Category>();
                FileManager.CreateCategoriesObservableCollection();

                // Get all Categories and show them
                foreach (Category cat in (App.Current as App)._itemViewHolder.Categories)
                {
                    categories.Add(cat);
                }
            });
            Bindings.Update();
        }
        
        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            AddButton.IsEnabled = false;
            AddCategoryButton.IsEnabled = false;
            ProgressRing.IsActive = true;

            Category category = null;
            if(CategoriesListView.SelectedIndex != 0)
            {
                category = CategoriesListView.SelectedItem as Category;
            }

            if (items.Count > 0)
            {
                foreach (StorageFile storagefile in items)
                {
                    if (storagefile.ContentType == "audio/wav" || storagefile.ContentType == "audio/mpeg")
                    {
                        FileManager.AddSound(Guid.Empty, storagefile.DisplayName, category.Uuid, storagefile);
                    }
                }
                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    (App.Current as App)._itemViewHolder.AllSoundsChanged = true;
                });
            }
            shareOperation.ReportCompleted();

            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                await FileManager.UpdateGridView();
            });
        }
        
        private void CategoriesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!ProgressRing.IsActive)
            {
                AddButton.IsEnabled = true;
            }
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

            categories.Add(FileManager.AddCategory(Guid.Empty, category.Name, category.Icon));
            Bindings.Update();

            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                FileManager.CreateCategoriesObservableCollection();
            });
        }
    }
}
