using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using UniversalSoundBoard.Model;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Search;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using static UniversalSoundBoard.Model.Sound;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace UniversalSoundBoard
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
       // public event EventHandler<BackRequestedEventArgs> onBackRequested;
        List<string> Suggestions;
        TextBox NewCategoryTextBox;
        TextBox EditCategoryTextBox;
        ComboBox IconSelectionComboBox;
        ContentDialog NewCategoryContentDialog;
        ContentDialog EditCategoryContentDialog;
        ObservableCollection<Category> Categories;

        public MainPage()
        {
            this.InitializeComponent();
            Loaded += MainPage_Loaded;
            SystemNavigationManager.GetForCurrentView().BackRequested += onBackRequested;

            BackButton.Visibility = Visibility.Collapsed;
            Suggestions = new List<string>();
            CreateCategoriesObservableCollection();
        }

        async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            setDataContext();
            await SoundManager.GetAllSounds();
        }

        private void setDataContext()
        {
            ContentRoot.DataContext = (App.Current as App)._itemViewHolder;
        }

        private async void CreateCategoriesObservableCollection()
        {
            Categories = new ObservableCollection<Category>();
            Categories.Add(new Category { Name = "Home", Icon = "\uE10F" });
            await FileManager.GetCategoriesListAsync();
            foreach(Category cat in (App.Current as App)._itemViewHolder.categories)
            {
                Categories.Add(cat);
            }
        }

        private void HamburgerButton_Click(object sender, RoutedEventArgs e)
        {
            SideBar.IsPaneOpen = !SideBar.IsPaneOpen;
        }

        private async void SearchAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            string text = sender.Text;
            if (String.IsNullOrEmpty(text)) goBack();

            (App.Current as App)._itemViewHolder.title = text;
            (App.Current as App)._itemViewHolder.searchQuery = text;
            await SoundManager.GetSoundsByName(text);
            Suggestions = (App.Current as App)._itemViewHolder.sounds.Where(p => p.Name.StartsWith(text)).Select(p => p.Name).ToList();
            SearchAutoSuggestBox.ItemsSource = Suggestions;
            BackButton.Visibility = Visibility.Visible;
            (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;
        }

        private async void SearchAutoSuggestBox_TextChanged_Mobile(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            string text = sender.Text;
            if (String.IsNullOrEmpty(text)) goBack();

            (App.Current as App)._itemViewHolder.title = text;
            (App.Current as App)._itemViewHolder.searchQuery = text;
            await SoundManager.GetSoundsByName(text);
            Suggestions = (App.Current as App)._itemViewHolder.sounds.Where(p => p.Name.StartsWith(text)).Select(p => p.Name).ToList();
            SearchAutoSuggestBox.ItemsSource = Suggestions;
            BackButton.Visibility = Visibility.Visible;
            (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;
        }

        private async void SearchAutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            string text = sender.Text;
            await SoundManager.GetSoundsByName(text);
            (App.Current as App)._itemViewHolder.title = text;
            (App.Current as App)._itemViewHolder.searchQuery = text;
            (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;
        }

        private async void SearchAutoSuggestBox_QuerySubmitted_Mobile(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            string text = sender.Text;
            await SoundManager.GetSoundsByName(text);
            (App.Current as App)._itemViewHolder.title = text;
            (App.Current as App)._itemViewHolder.searchQuery = text;
            (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            goBack();
        }

        private void BackButton_Click_Mobile(object sender, RoutedEventArgs e)
        {
            goBack_Mobile();
        }

        private void IconsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void SoundGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var sound = (Sound)e.ClickedItem;
           // MyMediaElement.Source = new Uri(this.BaseUri, sound.AudioFile);
            (App.Current as App)._itemViewHolder.mediaElementSource = new Uri(this.BaseUri, sound.AudioFile.Path);
        }

        private async void SoundGridView_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();

                (App.Current as App)._itemViewHolder.progressRingIsActive = true;
                if (items.Any())
                {
                    var storageFile = items[0] as StorageFile;
                    var contentType = storageFile.ContentType;

                    StorageFolder folder = ApplicationData.Current.LocalFolder;

                    if (items.Count > 0)
                    {
                        // Application now has read/write access to the picked file(s)
                        foreach (StorageFile sound in items)
                        {
                            if (contentType == "audio/wav" || contentType == "audio/mpeg")
                            {
                                await addSound(storageFile);
                            }
                        }
                        await FileManager.UpdateGridView();
                    }
                }
                (App.Current as App)._itemViewHolder.progressRingIsActive = false;
            }
        }

        private void SoundGridView_DragOver(object sender, DragEventArgs e)
        {
            // using Windows.ApplicationModel.DataTransfer;
            e.AcceptedOperation = DataPackageOperation.Copy;

            // Drag adorner ... change what mouse / icon looks like
            // as you're dragging the file into the app:
            // http://igrali.com/2015/05/15/drag-and-drop-photos-into-windows-10-universal-apps/
            e.DragUIOverride.Caption = "Drop to create a custom sound and tile";
            e.DragUIOverride.IsCaptionVisible = true;
            e.DragUIOverride.IsContentVisible = true;
            e.DragUIOverride.IsGlyphVisible = true;
            //e.DragUIOverride.SetContentFromBitmapImage(new BitmapImage(new Uri("ms-appx:///Assets/clippy.jpg")));
        }

        private async void goBack()
        {
            await SoundManager.GetAllSounds();
            Title.Text = "All Sounds";
            BackButton.Visibility = Visibility.Collapsed;
            (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;
            MenuItemsListView.SelectedItem = Categories.First();
            SearchAutoSuggestBox.Text = "";
        }

        private async void goBack_Mobile()
        {
            await SoundManager.GetAllSounds();
            (App.Current as App)._itemViewHolder.title = "All Sounds";
            MenuItemsListView.SelectedItem = Categories.First();

            SearchAutoSuggestBox.Text = "";
            SearchAutoSuggestBox.Visibility = Visibility.Collapsed;

            BackButton.Visibility = Visibility.Collapsed;
            AddButton.Visibility = Visibility.Visible;
            SearchButton.Visibility = Visibility.Visible;
            (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;
        }

        private void onBackRequested(object sender, BackRequestedEventArgs e)
        {
            e.Handled = true;

            if(BackButton.Visibility == Visibility.Collapsed)
            {
                App.Current.Exit();
            }

            if (Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile")
            {
                goBack_Mobile();
            }
            else
            {
                goBack();
            }
        }

        public void chooseGoBack()
        {
            if (Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile")
            {
                goBack_Mobile();
            }
            else
            {
                goBack();
            }
        }

        private async Task addSound(StorageFile file)
        {
            StorageFolder folder = ApplicationData.Current.LocalFolder;

            StorageFile newFile = await file.CopyAsync(folder, file.Name, NameCollisionOption.GenerateUniqueName);
            await FileManager.createSoundDetailsFileIfNotExistsAsync(file.DisplayName);

            MyMediaElement.SetSource(await file.OpenAsync(FileAccessMode.Read), file.ContentType);
            MyMediaElement.Play();
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            BackButton.Visibility = Visibility.Visible;
            AddButton.Visibility = Visibility.Collapsed;
            SearchButton.Visibility = Visibility.Collapsed;
            SearchAutoSuggestBox.Visibility = Visibility.Visible;

            // slightly delay setting focus
            Task.Factory.StartNew(
                () => Dispatcher.RunAsync(CoreDispatcherPriority.Low,
                    () => SearchAutoSuggestBox.Focus(FocusState.Programmatic)));
        }

        private async void NewSoundFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            // Open file explorer
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation =
                Windows.Storage.Pickers.PickerLocationId.MusicLibrary;
            picker.FileTypeFilter.Add(".mp3");
            picker.FileTypeFilter.Add(".wav");

            var files = await picker.PickMultipleFilesAsync();
            (App.Current as App)._itemViewHolder.progressRingIsActive = true;

            if (files.Count > 0)
            {
                // Application now has read/write access to the picked file(s)
                foreach (StorageFile sound in files)
                {
                    await addSound(sound);
                }
                // Reload page
                this.Frame.Navigate(this.GetType());
            }

            (App.Current as App)._itemViewHolder.progressRingIsActive = false;
        }

        private async void NewCategoryFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            NewCategoryContentDialog = new ContentDialog
            {
                Title = "New Category",
                PrimaryButtonText = "Create",
                SecondaryButtonText = "Cancel",
                IsPrimaryButtonEnabled = false
            };
            NewCategoryContentDialog.PrimaryButtonClick += NewCategoryContentDialog_PrimaryButtonClick;

            StackPanel stackPanel = new StackPanel();
            stackPanel.Orientation = Orientation.Vertical;

            List<string> IconsList = createIconsList();

            Random random = new Random();
            int randomNumber = random.Next(IconsList.Count);

            NewCategoryTextBox = new TextBox { Width = 300 };
            NewCategoryTextBox.Text = "";

            IconSelectionComboBox = new ComboBox
            {
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 25,
                Margin = new Thickness(15),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            foreach(string icon in IconsList)
            {
                IconSelectionComboBox.Items.Add(new ComboBoxItem { Content = icon, FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = 25 });
            }
            IconSelectionComboBox.SelectedIndex = randomNumber;

            stackPanel.Children.Add(NewCategoryTextBox);
            stackPanel.Children.Add(IconSelectionComboBox);

            NewCategoryContentDialog.Content = stackPanel;

            NewCategoryTextBox.TextChanged += NewCategoryContentDialogTextBox_TextChanged;

            await NewCategoryContentDialog.ShowAsync();
        }

        private async void NewCategoryContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Get categories List and save with new value
            List<Category> categoriesList = await FileManager.GetCategoriesListAsync();

            // Get combobox value
            ComboBoxItem typeItem = (ComboBoxItem)IconSelectionComboBox.SelectedItem;
            string icon = typeItem.Content.ToString();

            Category category = new Category
            {
                Name = NewCategoryTextBox.Text,
                Icon = icon
            };

            categoriesList.Add(category);
            await FileManager.SaveCategoriesListAsync(categoriesList);

            // Reload page
            this.Frame.Navigate(this.GetType());
        }

        private void NewCategoryContentDialogTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (NewCategoryTextBox.Text.Length < 3)
            {
                NewCategoryContentDialog.IsPrimaryButtonEnabled = false;
            }
            else
            {
                NewCategoryContentDialog.IsPrimaryButtonEnabled = true;
            }
        }

        private async void MenuItemsListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var category = (Category)e.ClickedItem;
            // Display all Sounds with the selected category
            if(category == Categories.First())
            {
                chooseGoBack();
            }
            else
            {
                await SoundManager.GetSoundsByCategory(category);
                (App.Current as App)._itemViewHolder.title = WebUtility.HtmlDecode(category.Name);
                BackButton.Visibility = Visibility.Visible;
                (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Visible;
            }
            SideBar.IsPaneOpen = false;
        }

        private List<string> createIconsList()
        {
            List<string> Icons = new List<string>();
            Icons.Add("\uE707");
            Icons.Add("\uE70F");
            Icons.Add("\uE710");
            Icons.Add("\uE711");
            Icons.Add("\uE713");
            Icons.Add("\uE714");
            Icons.Add("\uE715");
            Icons.Add("\uE716");
            Icons.Add("\uE717");
            Icons.Add("\uE718");
            Icons.Add("\uE719");
            Icons.Add("\uE71B");
            Icons.Add("\uE71C");
            Icons.Add("\uE71E");
            Icons.Add("\uE720");
            Icons.Add("\uE722");
            Icons.Add("\uE723");
            Icons.Add("\uE72C");
            Icons.Add("\uE72D");
            Icons.Add("\uE730");
            Icons.Add("\uE734");
            Icons.Add("\uE735");
            Icons.Add("\uE73A");
            Icons.Add("\uE73E");
            Icons.Add("\uE74D");
            Icons.Add("\uE74E");
            Icons.Add("\uE74F");
            Icons.Add("\uE753");
            Icons.Add("\uE765");
            Icons.Add("\uE767");
            Icons.Add("\uE768");
            Icons.Add("\uE769");
            Icons.Add("\uE76E");
            Icons.Add("\uE774");
            Icons.Add("\uE77A");
            Icons.Add("\uE77B");
            Icons.Add("\uE77F");
            Icons.Add("\uE786");
            Icons.Add("\uE7AD");
            Icons.Add("\uE7C1");
            Icons.Add("\uE7C3");
            Icons.Add("\uE7EE");
            Icons.Add("\uE7EF");
            Icons.Add("\uE80F");
            Icons.Add("\uE81D");
            Icons.Add("\uE890");
            Icons.Add("\uE894");
            Icons.Add("\uE895");
            Icons.Add("\uE896");
            Icons.Add("\uE897");
            Icons.Add("\uE899");
            Icons.Add("\uE8AA");
            Icons.Add("\uE8B1");
            Icons.Add("\uE8B8");
            Icons.Add("\uE8BD");
            Icons.Add("\uE8C3");
            Icons.Add("\uE8C6");
            Icons.Add("\uE8C9");
            Icons.Add("\uE8D6");
            Icons.Add("\uE8D7");
            Icons.Add("\uE8E1");
            Icons.Add("\uE8E0");
            Icons.Add("\uE8EA");
            Icons.Add("\uE8EB");
            Icons.Add("\uE8EC");
            Icons.Add("\uE8EF");
            Icons.Add("\uE8F0");
            Icons.Add("\uE8F1");
            Icons.Add("\uE8F3");
            Icons.Add("\uE8FB");
            Icons.Add("\uE909");
            Icons.Add("\uE90A");
            Icons.Add("\uE90B");
            Icons.Add("\uE90F");
            Icons.Add("\uE910");
            Icons.Add("\uE913");

            return Icons;
        }

        private async void CategoryEditButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            await ShowEditCategoryMessageDialog();
        }

        private async Task ShowEditCategoryMessageDialog()
        {
            EditCategoryContentDialog = new ContentDialog
            {
                Title = "Edit Category",
                PrimaryButtonText = "Save",
                SecondaryButtonText = "Cancel",
            };
            EditCategoryContentDialog.PrimaryButtonClick += EditCategoryContentDialog_PrimaryButtonClick;

            StackPanel stackPanel = new StackPanel();
            stackPanel.Orientation = Orientation.Vertical;

            List<string> IconsList = createIconsList();

            EditCategoryTextBox = new TextBox { Width = 300 };
            EditCategoryTextBox.Text = (App.Current as App)._itemViewHolder.title;

            IconSelectionComboBox = new ComboBox
            {
                FontFamily = new FontFamily("Segoe MDL2 Assets"),
                FontSize = 25,
                Margin = new Thickness(15),
                HorizontalAlignment = HorizontalAlignment.Center
            };

            foreach (string icon in IconsList)
            {
                ComboBoxItem item = new ComboBoxItem { Content = icon, FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = 25 };
                if (icon == (await FileManager.GetCategoryByNameAsync((App.Current as App)._itemViewHolder.title)).Icon){
                    item.IsSelected = true;
                }
                IconSelectionComboBox.Items.Add(item);
            }

            stackPanel.Children.Add(EditCategoryTextBox);
            stackPanel.Children.Add(IconSelectionComboBox);

            EditCategoryContentDialog.Content = stackPanel;

            EditCategoryTextBox.TextChanged += EditCategoryTextBox_TextChanged;

            await EditCategoryContentDialog.ShowAsync();
        }

        private void EditCategoryTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (EditCategoryTextBox.Text.Length < 3)
            {
                EditCategoryContentDialog.IsPrimaryButtonEnabled = false;
            }
            else
            {
                EditCategoryContentDialog.IsPrimaryButtonEnabled = true;
            }
        }

        private async void EditCategoryContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Get categories List and save with new value
            List<Category> categoriesList = await FileManager.GetCategoriesListAsync();

            // Get combobox value
            ComboBoxItem typeItem = (ComboBoxItem)IconSelectionComboBox.SelectedItem;
            string icon = typeItem.Content.ToString();

            string newName = EditCategoryTextBox.Text;
            string oldName = (App.Current as App)._itemViewHolder.title;

            categoriesList.Find(p => p.Name == oldName).Icon = icon;
            categoriesList.Find(p => p.Name == oldName).Name = newName;

            await FileManager.SaveCategoriesListAsync(categoriesList);
            await FileManager.renameCategory(oldName, newName);

            // Reload page
            this.Frame.Navigate(this.GetType());
            (App.Current as App)._itemViewHolder.title = "All Sounds";
            (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;
        }

        private async void CategoryDeleteButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            ContentDialog DeleteContentDialog = new ContentDialog
            {
                Title = "Delete category " + (App.Current as App)._itemViewHolder.title,
                Content = "Are you sure? Sounds are not impacted.",
                PrimaryButtonText = "Delete",
                SecondaryButtonText = "Cancel"
            };
            DeleteContentDialog.PrimaryButtonClick += DeleteContentDialog_PrimaryButtonClick;

            await DeleteContentDialog.ShowAsync();
        }

        private async void DeleteContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            await FileManager.deleteCategory((App.Current as App)._itemViewHolder.title);

            // Reload page
            this.Frame.Navigate(this.GetType());
        }
    }
}
