using System;
using System.Linq;
using UniversalSoundBoard.Models;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI;
using Windows.ApplicationModel.Core;
using Windows.UI.ViewManagement;
using UniversalSoundBoard.DataAccess;
using Windows.UI.Xaml.Media;
using UniversalSoundBoard.Common;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using WinUI = Microsoft.UI.Xaml.Controls;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using UniversalSoundboard.Components;
using UniversalSoundboard.Models;

namespace UniversalSoundBoard.Pages
{
    public sealed partial class MainPage : Page
    {
        readonly ResourceLoader loader = new ResourceLoader();
        public static CoreDispatcher dispatcher;    // Dispatcher for ShareTargetPage
        private Guid selectedCategory = Guid.Empty; // The category that was right clicked for the flyout
        private List<string> Suggestions = new List<string>();  // The suggestions for the SearchAutoSuggestBox
        private List<StorageFile> sharedFiles = new List<StorageFile>();    // The files that get shared
        bool selectionButtonsEnabled = false;               // If true, the buttons for multi selection are enabled
        private bool downloadFileIsExecuting = false;
        private bool downloadFileWasCanceled = false;
        private bool downloadFileThrewError = false;

        public MainPage()
        {
            InitializeComponent();
            SetThemeColors();

            RootGrid.DataContext = FileManager.itemViewHolder;
            dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;

            SystemNavigationManager.GetForCurrentView().BackRequested += MainPage_BackRequested;
            FileManager.itemViewHolder.PropertyChanged += ItemViewHolder_PropertyChanged;
            FileManager.itemViewHolder.SelectedSounds.CollectionChanged += SelectedSounds_CollectionChanged;
            FileManager.itemViewHolder.CategoriesUpdatedEvent += ItemViewHolder_CategoriesUpdatedEvent;
            FileManager.itemViewHolder.CategoryUpdatedEvent += ItemViewHolder_CategoryUpdatedEvent;
            FileManager.itemViewHolder.CategoryRemovedEvent += ItemViewHolder_CategoryRemovedEvent;
        }

        #region Page event handlers
        async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            AdjustLayout();

            // Load the Categories and the menu items
            await FileManager.LoadCategoriesAsync();
            LoadMenuItems();

            // Load the PlayingSounds
            await FileManager.LoadPlayingSoundsAsync();

            // Load the Sounds
            await FileManager.ShowAllSoundsAsync();

            // Load the user details and start the sync
            await FileManager.itemViewHolder.User.InitAsync();

            // Set the values of the volume sliders
            VolumeSlider.Value = FileManager.itemViewHolder.Volume * 100;
        }

        async void MainPage_BackRequested(object sender, BackRequestedEventArgs e)
        {
            await GoBack();
            e.Handled = true;
        }

        private void MainPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            AdjustLayout();
        }

        private void ItemViewHolder_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("Page") || e.PropertyName.Equals("AppState"))
            {
                bool navigationViewHeaderVisible = FileManager.itemViewHolder.AppState == FileManager.AppState.Normal || FileManager.itemViewHolder.Page != typeof(SoundPage);
                NavigationViewHeader.Visibility = navigationViewHeaderVisible ? Visibility.Visible : Visibility.Collapsed;
            }
            else if(e.PropertyName.Equals("CurrentTheme"))
                SetThemeColors();
        }

        private void SelectedSounds_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            selectionButtonsEnabled = FileManager.itemViewHolder.SelectedSounds.Count > 0;
            Bindings.Update();
        }

        private void ItemViewHolder_CategoriesUpdatedEvent(object sender, EventArgs e)
        {
            LoadMenuItems();
        }

        private async void ItemViewHolder_CategoryUpdatedEvent(object sender, Guid uuid)
        {
            // Update the text and icon of the menu item of the category
            UpdateCategoryMenuItem(SideBar.MenuItems, await FileManager.GetCategoryAsync(uuid));
        }

        private void ItemViewHolder_CategoryRemovedEvent(object sender, Guid uuid)
        {
            // Remove the category from the SideBar
            RemoveCategoryMenuItem(SideBar.MenuItems, uuid);
        }
        #endregion

        private void CustomiseTitleBar()
        {
            ApplicationViewTitleBar titleBar = ApplicationView.GetForCurrentView().TitleBar;
            titleBar.ButtonForegroundColor = FileManager.itemViewHolder.CurrentTheme == FileManager.AppTheme.Dark ? Colors.White : Colors.Black;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

            // Set custom title bar
            CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;
            Window.Current.SetTitleBar(TitleBar);
        }

        private void InitLayout()
        {
            SideBar.ExpandedModeThresholdWidth = FileManager.sideBarCollapsedMaxWidth;

            // Set the background of the sidebar content
            SideBar.Background = new SolidColorBrush(FileManager.GetApplicationThemeColor());

            // Initialize the acrylic background of the SideBar
            Application.Current.Resources["NavigationViewExpandedPaneBackground"] = new AcrylicBrush();

            FileManager.UpdateLayoutColors();
        }

        private void AdjustLayout()
        {
            FileManager.AdjustLayout();

            // Set the width of the title bar and the position of the title, depending on whether the Hamburger button of the NavigationView is visible
            if (SideBar.DisplayMode == WinUI.NavigationViewDisplayMode.Minimal)
            {
                TitleBar.Width = Window.Current.Bounds.Width - 80;
                WindowTitleTextBlock.Margin = new Thickness(97, 12, 0, 0);
            }
            else
            {
                TitleBar.Width = Window.Current.Bounds.Width - 40;
                WindowTitleTextBlock.Margin = new Thickness(57, 12, 0, 0);
            }
        }

        private void SetThemeColors()
        {
            InitLayout();
            RequestedTheme = FileManager.GetRequestedTheme();
            CustomiseTitleBar();
        }

        private void LoadMenuItems()
        {
            SideBar.MenuItems.Clear();

            foreach (var menuItem in CreateMenuItemsforCategories(FileManager.itemViewHolder.Categories.ToList()))
                SideBar.MenuItems.Add(menuItem);
        }

        private List<WinUI.NavigationViewItem> CreateMenuItemsforCategories(List<Category> categories)
        {
            List<WinUI.NavigationViewItem> menuItems = new List<WinUI.NavigationViewItem>();

            foreach (var category in categories)
            {
                WinUI.NavigationViewItem item = new WinUI.NavigationViewItem
                {
                    Tag = category.Uuid,
                    Content = category.Name,
                    Icon = new FontIcon { Glyph = category.Icon }
                };

                item.RightTapped += NavigationViewMenuItem_RightTapped;

                foreach (var childItem in CreateMenuItemsforCategories(category.Children.ToList()))
                    item.MenuItems.Add(childItem);

                menuItems.Add(item);
            }

            return menuItems;
        }

        private bool AddCategoryMenuItem(IList<object> menuItems, Category category, Guid parent)
        {
            if (parent.Equals(Guid.Empty))
            {
                // Add the category menu item to the end of the menuItems
                WinUI.NavigationViewItem item = new WinUI.NavigationViewItem
                {
                    Tag = category.Uuid,
                    Content = category.Name,
                    Icon = new FontIcon { Glyph = category.Icon }
                };

                item.RightTapped += NavigationViewMenuItem_RightTapped;
                menuItems.Add(item);
                return true;
            }

            // Find the MenuItem of the parent category
            foreach (var menuItem in menuItems)
            {
                WinUI.NavigationViewItem item = (WinUI.NavigationViewItem)menuItem;

                if ((Guid)item.Tag == parent)
                {
                    WinUI.NavigationViewItem newItem = new WinUI.NavigationViewItem
                    {
                        Tag = category.Uuid,
                        Content = category.Name,
                        Icon = new FontIcon { Glyph = category.Icon }
                    };

                    newItem.RightTapped += NavigationViewMenuItem_RightTapped;
                    item.MenuItems.Add(newItem);
                    return true;
                }
                else
                {
                    if (AddCategoryMenuItem(item.MenuItems, category, parent))
                        return true;
                }
            }

            return false;
        }

        private bool UpdateCategoryMenuItem(IList<object> menuItems, Category updatedCategory)
        {
            foreach(var menuItem in menuItems)
            {
                WinUI.NavigationViewItem item = (WinUI.NavigationViewItem)menuItem;

                if((Guid)item.Tag == updatedCategory.Uuid)
                {
                    item.Content = updatedCategory.Name;
                    item.Icon = new FontIcon { Glyph = updatedCategory.Icon };
                    return true;
                }
                else
                {
                    if (UpdateCategoryMenuItem(item.MenuItems, updatedCategory))
                        return true;
                }
            }

            return false;
        }

        private bool RemoveCategoryMenuItem(IList<object> menuItems, Guid uuid)
        {
            bool categoryFound = false;
            int i = 0;

            foreach(var menuItem in menuItems)
            {
                WinUI.NavigationViewItem item = (WinUI.NavigationViewItem)menuItem;

                if((Guid)item.Tag == uuid)
                {
                    categoryFound = true;
                    break;
                }
                else
                {
                    if (RemoveCategoryMenuItem(item.MenuItems, uuid))
                        return true;
                }

                i++;
            }

            if (categoryFound)
            {
                // Remove the menu item
                menuItems.RemoveAt(i);
                return true;
            }
            return false;
        }

        /**
         * Returns a list of the positions of the menu item parents and the category itself of the searched category in the correct order
         */
        private List<int> GetCategoryMenuItemPositionPath(Guid searchedCategoryUuid)
        {
            List<int> positions = new List<int>();
            List<WinUI.NavigationViewItem> parentItems = new List<WinUI.NavigationViewItem>();
            int i = 0;

            foreach(var menuItem in SideBar.MenuItems)
            {
                WinUI.NavigationViewItem item = (WinUI.NavigationViewItem)menuItem;
                Guid uuid = (Guid)item.Tag;
                if (uuid.Equals(Guid.Empty)) continue;

                positions.Add(i);
                parentItems.Add(item);
                if (BuildCategoryMenuItemPositionPath(positions, parentItems, searchedCategoryUuid))
                    return positions;
                else
                {
                    // Remove the position and the item from positions and from parentItems
                    positions.RemoveAt(positions.Count - 1);
                    parentItems.Remove(item);
                }

                i++;
            }

            return positions;
        }

        /**
         * Goes through the nested menu items tree and builds the path of the positions in positions and the path of the menu items in parentItems
         * Returns true if the last menu item belongs to the searched category or contains the menu item of it within the child tree
         */
        private bool BuildCategoryMenuItemPositionPath(List<int> positions, List<WinUI.NavigationViewItem> parentItems, Guid searchedCategoryUuid)
        {
            // Check if the last item in parentItems is the searched category
            if (((Guid)parentItems.Last().Tag).Equals(searchedCategoryUuid)) return true;
            int i = 0;

            // Add each child of the last element in parentItems to parentItems and call this method
            foreach(var childItem in parentItems.Last().MenuItems)
            {
                WinUI.NavigationViewItem item = (WinUI.NavigationViewItem)childItem;

                positions.Add(i);
                parentItems.Add(item);

                if (BuildCategoryMenuItemPositionPath(positions, parentItems, searchedCategoryUuid))
                    return true;
                else
                {
                    // Remove the position and the item from positions and from parentItems
                    positions.RemoveAt(positions.Count - 1);
                    parentItems.Remove(item);
                }

                i++;
            }

            return false;
        }

        /**
         * Returns the count of the children of the category, defined by the given path
         */
        private int GetCategoryMenuItemChildrenCountByPath(List<int> positionPath)
        {
            List<WinUI.NavigationViewItem> currentItemList = new List<WinUI.NavigationViewItem>();
            
            // Copy the SideBar menu items into the current items list
            for(int i = 1; i < SideBar.MenuItems.Count; i++)
                currentItemList.Add((WinUI.NavigationViewItem)SideBar.MenuItems[i]);

            foreach (var position in positionPath)
            {
                var currentItem = currentItemList.ElementAt(position);
                currentItemList.Clear();

                foreach (var childItem in currentItem.MenuItems)
                    currentItemList.Add((WinUI.NavigationViewItem)childItem);
            }

            return currentItemList.Count;
        }

        /**
         * Finds the category menu item with the given uuid and moves it up or down within the list of menu items
         * Should be called with menuItems = SideBar.MenuItems
         */
        private bool MoveCategoryMenuItem(IList<object> menuItems, Guid searchedCategoryUuid, bool up)
        {
            bool categoryFound = false;
            int i = 0;

            foreach (var menuItem in menuItems)
            {
                WinUI.NavigationViewItem item = (WinUI.NavigationViewItem)menuItem;

                if (((Guid)item.Tag).Equals(searchedCategoryUuid))
                {
                    categoryFound = true;
                    break;
                }
                else
                {
                    if (MoveCategoryMenuItem(item.MenuItems, searchedCategoryUuid, up))
                        return true;
                }

                i++;
            }

            if (categoryFound)
            {
                // Move the menu item
                var selectedItem = menuItems[i];
                menuItems.Remove(selectedItem);
                if (up)
                    menuItems.Insert(i - 1, selectedItem);
                else
                    menuItems.Insert(i + 1, selectedItem);

                return true;
            }
            return false;
        }

        /**
         * Finds the category menu item with the given uuid and moves it into the children of the menu item above or below
         * Should be called with menuItems = SideBar.MenuItems
         */
        private bool MoveCategoryMenuItemToMenuItem(IList<object> menuItems, Guid searchedCategoryUuid, bool up)
        {
            for (int i = 0; i < menuItems.Count; i++)
            {
                var item = (WinUI.NavigationViewItem)menuItems[i];

                if (((Guid)item.Tag).Equals(searchedCategoryUuid))
                {
                    var movedElement = menuItems.ElementAt(i);

                    // Remove the item from the menu items
                    menuItems.RemoveAt(i);

                    // Add the menu item to the category above or below
                    if (up)
                        ((WinUI.NavigationViewItem)menuItems[i - 1]).MenuItems.Add(movedElement);
                    else
                        ((WinUI.NavigationViewItem)menuItems[i]).MenuItems.Insert(0, movedElement);

                    return true;
                }
                else if (MoveCategoryMenuItemToMenuItem(item.MenuItems, searchedCategoryUuid, up))
                    return true;
            }
            return false;
        }

        /**
         * Finds the category menu item with the given uuid and moves it into the children of the parent menu item above or below
         * Should be called with menuItems = SideBar.MenuItems
         */
        private bool MoveCategoryMenuItemToParent(IList<object> menuItems, Guid uuid, bool up)
        {
            for(int i = 0; i < menuItems.Count; i++)
            {
                var item = (WinUI.NavigationViewItem)menuItems[i];
                bool categoryFound = false;
                int j = 0;

                foreach(var childMenuItem in item.MenuItems)
                {
                    var childItem = (WinUI.NavigationViewItem)childMenuItem;

                    if(((Guid)childItem.Tag).Equals(uuid))
                    {
                        categoryFound = true;
                        break;
                    }
                    
                    j++;
                }

                if (categoryFound)
                {
                    var movedElement = item.MenuItems.ElementAt(j);

                    // Remove the child from the children of the menu item
                    item.MenuItems.RemoveAt(j);

                    // Add the element to the parent
                    if(up)
                        menuItems.Insert(i, movedElement);
                    else
                        menuItems.Insert(i + 1, movedElement);

                    return true;
                }
                else if (MoveCategoryMenuItemToParent(item.MenuItems, uuid, up))
                    return true;
            }
            return false;
        }

        private async Task GoBack()
        {
            await FileManager.GoBackAsync();
            AdjustLayout();
        }

        #region SideBar
        private async void SideBar_ItemInvoked(WinUI.NavigationView sender, WinUI.NavigationViewItemInvokedEventArgs args)
        {
            FileManager.itemViewHolder.SelectedSounds.Clear();
            FileManager.ResetSearchArea();

            if (args.IsSettingsInvoked)
            {
                // Show the Settings page
                FileManager.NavigateToSettingsPage();
            }
            else
            {
                // Show the selected category
                if (args.InvokedItemContainer == null) return;
                Guid categoryUuid = (Guid)args.InvokedItemContainer.Tag;
                if (categoryUuid.Equals(FileManager.itemViewHolder.SelectedCategory) && FileManager.itemViewHolder.Page == typeof(SoundPage)) return;

                if (Equals(categoryUuid, Guid.Empty))
                    await FileManager.ShowAllSoundsAsync();
                else
                    await FileManager.ShowCategoryAsync(categoryUuid);
            }
        }

        private void LogInMenuItem_Tapped(object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            // Show the Account page
            FileManager.NavigateToAccountPage();

            // Close the SideBar if it was open on mobile
            if (
                SideBar.DisplayMode == WinUI.NavigationViewDisplayMode.Compact
                || SideBar.DisplayMode == WinUI.NavigationViewDisplayMode.Minimal
            ) SideBar.IsPaneOpen = false;
        }

        private void NavigationViewMenuItem_RightTapped(object sender, Windows.UI.Xaml.Input.RightTappedRoutedEventArgs e)
        {
            e.Handled = true;
            selectedCategory = (Guid)((WinUI.NavigationViewItem)sender).Tag;
            if (selectedCategory.Equals(Guid.Empty)) return;

            // Show flyout for the category menu item
            ShowCategoryOptionsFlyout((UIElement)sender, e.GetPosition(sender as UIElement));
        }

        private async void SideBar_BackRequested(WinUI.NavigationView sender, WinUI.NavigationViewBackRequestedEventArgs args)
        {
            await GoBack();
        }
        #endregion

        #region Edit Category
        private async void CategoryEditButton_Click(object sender, RoutedEventArgs e)
        {
            var editCategoryContentDialog = ContentDialogs.CreateEditCategoryContentDialog();
            editCategoryContentDialog.PrimaryButtonClick += EditCategoryContentDialog_PrimaryButtonClick;
            await editCategoryContentDialog.ShowAsync();
        }

        private async void EditCategoryContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Get categories List and save with new value
            // Get combobox value
            ComboBoxItem typeItem = (ComboBoxItem)ContentDialogs.IconSelectionComboBox.SelectedItem;
            string icon = typeItem.Content.ToString();
            string newName = ContentDialogs.EditCategoryTextBox.Text;

            // Update the category in the database
            await FileManager.UpdateCategoryAsync(FileManager.itemViewHolder.SelectedCategory, newName, icon);

            // Update the title and reload the category in the categories list
            FileManager.itemViewHolder.Title = newName;
            await FileManager.ReloadCategory(FileManager.itemViewHolder.SelectedCategory);
        }
        #endregion

        #region Delete Category
        private async void CategoryDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var deleteCategoryContentDialog = ContentDialogs.CreateDeleteCategoryContentDialogAsync();
            deleteCategoryContentDialog.PrimaryButtonClick += DeleteCategoryContentDialog_PrimaryButtonClick;
            await deleteCategoryContentDialog.ShowAsync();
        }

        private async void DeleteCategoryContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            await FileManager.DeleteCategoryAsync(FileManager.itemViewHolder.SelectedCategory);

            // Remove the category from the Categories list
            FileManager.RemoveCategory(FileManager.itemViewHolder.SelectedCategory);

            // Navigate to all Sounds
            await FileManager.ShowAllSoundsAsync();
        }
        #endregion

        #region Play All / Play Sounds Successively / Play Sounds Simultaneously
        private async void CategoryPlayAllButton_Click(object sender, RoutedEventArgs e)
        {
            List<Sound> sounds = new List<Sound>();

            // Check if it should play all sounds or the favourite sounds
            if (SoundPage.soundsPivotSelected || !FileManager.itemViewHolder.ShowSoundsPivot)
                foreach (Sound sound in FileManager.itemViewHolder.Sounds)
                    sounds.Add(sound);
            else
                foreach (Sound sound in FileManager.itemViewHolder.FavouriteSounds)
                    sounds.Add(sound);

            var template = (DataTemplate)Resources["SoundItemTemplate"];
            var listViewItemStyle = Resources["ListViewItemStyle"] as Style;

            var playSoundsSuccessivelyContentDialog = ContentDialogs.CreatePlaySoundsSuccessivelyContentDialog(sounds, template, listViewItemStyle);
            playSoundsSuccessivelyContentDialog.PrimaryButtonClick += PlaySoundsSuccessivelyContentDialog_PrimaryButtonClick;
            await playSoundsSuccessivelyContentDialog.ShowAsync();
        }

        private async void PlaySoundsSuccessivelyFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            List<Sound> sounds = new List<Sound>();
            foreach (Sound sound in FileManager.itemViewHolder.SelectedSounds)
                sounds.Add(sound);

            var template = (DataTemplate)Resources["SoundItemTemplate"];
            var listViewItemStyle = Resources["ListViewItemStyle"] as Style;

            var playSoundsSuccessivelyContentDialog = ContentDialogs.CreatePlaySoundsSuccessivelyContentDialog(sounds, template, listViewItemStyle);
            playSoundsSuccessivelyContentDialog.PrimaryButtonClick += PlaySoundsSuccessivelyContentDialog_PrimaryButtonClick;
            await playSoundsSuccessivelyContentDialog.ShowAsync();
        }

        private async void PlaySoundsSuccessivelyContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            bool randomly = (bool)ContentDialogs.RandomCheckBox.IsChecked;
            int rounds = int.MaxValue;

            if (ContentDialogs.RepeatsComboBox.SelectedItem != ContentDialogs.RepeatsComboBox.Items.Last())
                int.TryParse(ContentDialogs.RepeatsComboBox.SelectedValue.ToString(), out rounds);

            await SoundPage.PlaySoundsAsync(ContentDialogs.SoundsList.ToList(), rounds, randomly);
        }

        private async void PlaySoundsSimultaneouslyFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            bool oldPlayOneSoundAtOnce = FileManager.itemViewHolder.PlayOneSoundAtOnce;

            FileManager.itemViewHolder.PlayOneSoundAtOnce = false;
            foreach (Sound sound in FileManager.itemViewHolder.SelectedSounds)
                await SoundPage.PlaySoundAsync(sound);

            FileManager.itemViewHolder.PlayOneSoundAtOnce = oldPlayOneSoundAtOnce;
        }
        #endregion

        #region Volume Slider
        private void VolumeSlider_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            var volumeSlider = sender as Slider;
            double newValue = e.NewValue;
            double oldValue = e.OldValue;

            // Change Volume of MediaPlayers
            double addedValue = (newValue - oldValue) / 100;

            foreach (PlayingSound playingSound in FileManager.itemViewHolder.PlayingSounds)
            {
                if ((playingSound.MediaPlayer.Volume + addedValue) > 1)
                    playingSound.MediaPlayer.Volume = 1;
                else if ((playingSound.MediaPlayer.Volume + addedValue) < 0)
                    playingSound.MediaPlayer.Volume = 0;
                else
                    playingSound.MediaPlayer.Volume += addedValue;
            }

            // Save the new volume
            FileManager.itemViewHolder.Volume = volumeSlider.Value / 100;
        }

        private async void VolumeSlider_LostFocus(object sender, RoutedEventArgs e)
        {
            // Save the new volume of all playing sounds
            foreach (PlayingSound playingSound in FileManager.itemViewHolder.PlayingSounds)
                await FileManager.SetVolumeOfPlayingSoundAsync(playingSound.Uuid, playingSound.MediaPlayer.Volume);
        }
        #endregion

        #region New Sounds
        private async void NewSoundFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            await PickSounds();
        }

        public static async Task PickSounds()
        {
            var loader = new ResourceLoader();

            // Open file explorer
            var picker = new Windows.Storage.Pickers.FileOpenPicker
            {
                ViewMode = Windows.Storage.Pickers.PickerViewMode.List,
                SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.MusicLibrary
            };

            foreach (var fileType in FileManager.allowedFileTypes)
                picker.FileTypeFilter.Add(fileType);

            var files = await picker.PickMultipleFilesAsync();

            if (files.Any())
            {
                FileManager.itemViewHolder.LoadingScreenMessage = loader.GetString("AddSoundsMessage");
                FileManager.itemViewHolder.LoadingScreenVisible = true;

                // Get the category
                Guid? selectedCategory = null;
                if (!Equals(FileManager.itemViewHolder.SelectedCategory, Guid.Empty))
                    selectedCategory = FileManager.itemViewHolder.SelectedCategory;

                // Add all selected files
                foreach (StorageFile soundFile in files)
                {
                    // Add the sound to the sound lists
                    await FileManager.AddSound(await FileManager.CreateSoundAsync(null, soundFile.DisplayName, selectedCategory, soundFile));
                }
            }

            FileManager.itemViewHolder.LoadingScreenVisible = false;
            if (FileManager.itemViewHolder.AppState == FileManager.AppState.Empty && files.Any())
                FileManager.itemViewHolder.AppState = FileManager.AppState.Normal;
        }
        #endregion

        #region New Category
        private async void NewCategoryFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            var newCategoryContentDialog = ContentDialogs.CreateNewCategoryContentDialog(Guid.Empty);
            newCategoryContentDialog.PrimaryButtonClick += NewCategoryContentDialog_PrimaryButtonClick;
            await newCategoryContentDialog.ShowAsync();
        }

        private async void NewCategoryContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Get combobox value
            ComboBoxItem typeItem = (ComboBoxItem)ContentDialogs.IconSelectionComboBox.SelectedItem;
            string icon = typeItem.Content.ToString();

            Guid categoryUuid = await FileManager.CreateCategoryAsync(null, null, ContentDialogs.NewCategoryTextBox.Text, icon);
            Category newCategory = await FileManager.GetCategoryAsync(categoryUuid, false);

            // Add the category to the Categories list
            FileManager.AddCategory(newCategory, Guid.Empty);
            FileManager.itemViewHolder.TriggerCategoriesUpdatedEvent();

            // Add the category to the SideBar
            AddCategoryMenuItem(SideBar.MenuItems, newCategory, Guid.Empty);

            // Navigate to the new category
            await FileManager.ShowCategoryAsync(categoryUuid);
        }
        #endregion

        #region Search
        private async void SearchAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.ProgrammaticChange) return;

            string text = sender.Text;
            if (string.IsNullOrEmpty(text))
            {
                // Show all sounds
                await FileManager.ShowAllSoundsAsync();
            }
            else
            {
                // Show the search result
                FileManager.itemViewHolder.Title = text;
                FileManager.itemViewHolder.SearchQuery = text;
                FileManager.itemViewHolder.SelectedCategory = Guid.Empty;
                FileManager.itemViewHolder.BackButtonEnabled = true;
                FileManager.itemViewHolder.EditButtonVisible = false;

                // Update the suggestions
                Suggestions = FileManager.itemViewHolder.AllSounds.Where(p => p.Name.ToLower().StartsWith(text.ToLower())).Select(p => p.Name).ToList();

                // Load the searched sounds
                await FileManager.LoadSoundsByNameAsync(text);

                FileManager.UpdatePlayAllButtonVisibility();
                FileManager.UpdateBackButtonVisibility();
            }
        }

        private async void SearchAutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            string text = sender.Text;

            if (string.IsNullOrEmpty(text))
            {
                FileManager.itemViewHolder.Title = new ResourceLoader().GetString("AllSounds");
            }
            else
            {
                FileManager.itemViewHolder.Title = text;
                FileManager.itemViewHolder.SearchQuery = text;
                FileManager.itemViewHolder.EditButtonVisible = false;
                await FileManager.LoadSoundsByNameAsync(text);
            }

            FileManager.UpdatePlayAllButtonVisibility();
            FileManager.UpdateBackButtonVisibility();
        }

        private void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            FileManager.itemViewHolder.BackButtonEnabled = true;
            FileManager.itemViewHolder.SearchButtonVisible = false;
            FileManager.itemViewHolder.SearchAutoSuggestBoxVisible = true;

            // slightly delay setting focus
            Task.Factory.StartNew(
                () => Dispatcher.RunAsync(CoreDispatcherPriority.Low,
                    () => SearchAutoSuggestBox.Focus(FocusState.Programmatic)));
        }
        #endregion

        #region CancelButton
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            FileManager.itemViewHolder.MultiSelectionEnabled = false;
            AdjustLayout();
        }
        #endregion

        #region SelectButton
        private void SelectButton_Click(object sender, RoutedEventArgs e)
        {
            FileManager.itemViewHolder.MultiSelectionEnabled = true;
            AdjustLayout();
        }
        #endregion

        #region Select all
        private void MoreButton_SelectAllFlyout_Click(object sender, RoutedEventArgs e)
        {
            FileManager.itemViewHolder.TriggerSelectAllSoundsEvent(sender, e);
        }
        #endregion

        #region Share
        private async void MoreButton_ShareFlyout_Click(object sender, RoutedEventArgs e)
        {
            if (!await DownloadSelectedFiles()) return;

            sharedFiles.Clear();
            StorageFolder tempFolder = ApplicationData.Current.TemporaryFolder;

            // Copy the files into the temp folder
            foreach (Sound sound in FileManager.itemViewHolder.SelectedSounds)
            {
                StorageFile audioFile = await sound.GetAudioFileAsync();
                if (audioFile == null) return;

                string ext = await sound.GetAudioFileExtensionAsync();
                if (string.IsNullOrEmpty(ext)) ext = "mp3";

                StorageFile tempFile = await audioFile.CopyAsync(tempFolder, sound.Name + "." + ext, NameCollisionOption.ReplaceExisting);
                sharedFiles.Add(tempFile);
            }

            DataTransferManager dataTransferManager = DataTransferManager.GetForCurrentView();
            dataTransferManager.DataRequested += DataTransferManager_DataRequested;
            DataTransferManager.ShowShareUI();
        }

        private void DataTransferManager_DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            if (sharedFiles.Count == 0) return;

            string description = loader.GetString("ShareDialog-MultipleSounds");
            if (sharedFiles.Count == 1)
                description = sharedFiles.First().Name;

            DataRequest request = args.Request;
            request.Data.SetStorageItems(sharedFiles);
            request.Data.Properties.Title = loader.GetString("ShareDialog-Title");
            request.Data.Properties.Description = description;
        }
        #endregion

        #region File download
        private async Task<bool> DownloadSelectedFiles()
        {
            var selectedSounds = FileManager.itemViewHolder.SelectedSounds;

            // Download each file that is not available locally
            foreach (var sound in selectedSounds)
            {
                var downloadStatus = await sound.GetAudioFileDownloadStatusAsync();
                if (downloadStatus == DownloadStatus.NoFileOrNotLoggedIn) continue;

                if (downloadStatus != DownloadStatus.Downloaded)
                {
                    // Download the file and show the download dialog
                    downloadFileIsExecuting = true;
                    Progress<int> progress = new Progress<int>(FileDownloadProgress);
                    await sound.DownloadFileAsync(progress);

                    ContentDialogs.CreateDownloadFileContentDialog(sound.Name + "." + sound.GetAudioFileExtensionAsync());
                    ContentDialogs.downloadFileProgressBar.IsIndeterminate = true;
                    ContentDialogs.DownloadFileContentDialog.SecondaryButtonClick += DownloadFileContentDialog_SecondaryButtonClick;
                    await ContentDialogs.DownloadFileContentDialog.ShowAsync();
                }

                if (downloadFileWasCanceled)
                {
                    downloadFileWasCanceled = false;
                    return false;
                }

                if (downloadFileThrewError) break;
            }

            if (downloadFileThrewError)
            {
                var errorContentDialog = ContentDialogs.CreateDownloadFileErrorContentDialog();
                await errorContentDialog.ShowAsync();
                downloadFileThrewError = false;
                return false;
            }

            return true;
        }

        private void FileDownloadProgress(int value)
        {
            if (!downloadFileIsExecuting) return;

            if (value < 0)
            {
                // There was an error
                downloadFileThrewError = true;
                downloadFileIsExecuting = false;
                ContentDialogs.DownloadFileContentDialog.Hide();
            }
            else if (value > 100)
            {
                // Hide the download dialog
                ContentDialogs.DownloadFileContentDialog.Hide();
            }
        }

        private void DownloadFileContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            downloadFileWasCanceled = true;
            downloadFileIsExecuting = false;
        }
        #endregion

        #region Set categories
        private async void MoreButton_SetCategory_Click(object sender, RoutedEventArgs e)
        {
            // Show the Set Categories content dialog for multiple sounds
            List<Sound> selectedSounds = new List<Sound>();
            foreach (var sound in FileManager.itemViewHolder.SelectedSounds)
                selectedSounds.Add(sound);

            var SetCategoryContentDialog = ContentDialogs.CreateSetCategoriesContentDialog(selectedSounds);
            SetCategoryContentDialog.PrimaryButtonClick += SetCategoriesContentDialog_PrimaryButtonClick;
            await SetCategoryContentDialog.ShowAsync();
        }

        private async void SetCategoriesContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            FileManager.itemViewHolder.LoadingScreenMessage = loader.GetString("UpdateSoundsMessage");
            FileManager.itemViewHolder.LoadingScreenVisible = true;

            // Get the selected categories
            List<Guid> categoryUuids = new List<Guid>();
            foreach (var item in ContentDialogs.CategoriesTreeView.SelectedItems)
                categoryUuids.Add((Guid)((CustomTreeViewNode)item).Tag);

            // Get the selected sounds
            List<Sound> selectedSounds = new List<Sound>();
            foreach (var sound in FileManager.itemViewHolder.SelectedSounds)
                selectedSounds.Add(sound);

            // Update and reload the sounds
            foreach (var sound in selectedSounds)
            {
                await FileManager.SetCategoriesOfSoundAsync(sound.Uuid, categoryUuids);
                await FileManager.ReloadSound(sound.Uuid);
            }

            FileManager.itemViewHolder.LoadingScreenVisible = false;
        }
        #endregion

        #region CategoryOptionsFlyout
        private void ShowCategoryOptionsFlyout(UIElement sender, Point position)
        {
            // Get the position of the selected category
            List<int> positionPath = GetCategoryMenuItemPositionPath(selectedCategory);
            int itemPosition = positionPath.ElementAt(positionPath.Count - 1);
            positionPath.RemoveAt(positionPath.Count - 1);

            int selectedItemChildCount = GetCategoryMenuItemChildrenCountByPath(positionPath);
            bool isFirstItem = itemPosition == 0;
            bool isLastItem = itemPosition == selectedItemChildCount - 1;
            bool isSubCategory = positionPath.Count > 0;

            // Create and show the MenuFlyout
            MenuFlyout flyout = new MenuFlyout();

            // Position
            MenuFlyoutSubItem positionSubFlyoutItem = new MenuFlyoutSubItem { Text = loader.GetString("CategoryOptionsFlyout-Position") };

            FontIcon arrowTop = new FontIcon { Glyph = "\uE74A" };
            FontIcon arrowBottom = new FontIcon { Glyph = "\uE74B" };
            FontIcon arrowTopLeft = new FontIcon { Glyph = "\uE742" };
            FontIcon arrowTopRight = new FontIcon { Glyph = "\uE742", FlowDirection = FlowDirection.RightToLeft, MirroredWhenRightToLeft = true };
            FontIcon arrowBottomLeft = new FontIcon { Glyph = "\uE741", FlowDirection = FlowDirection.RightToLeft, MirroredWhenRightToLeft = true };
            FontIcon arrowBottomRight = new FontIcon { Glyph = "\uE741" };

            // Move up
            MenuFlyoutItem moveUpFlyoutItem = new MenuFlyoutItem { Text = loader.GetString("CategoryOptionsFlyout-Position-MoveUp") };
            moveUpFlyoutItem.Click += MoveUpFlyoutItem_Click;

            // Move down
            MenuFlyoutItem moveDownFlyoutItem = new MenuFlyoutItem { Text = loader.GetString("CategoryOptionsFlyout-Position-MoveDown") };
            moveDownFlyoutItem.Click += MoveDownFlyoutItem_Click;

            // Move to category above
            MenuFlyoutItem moveToCategoryAboveItem = new MenuFlyoutItem { Text = loader.GetString("CategoryOptionsFlyout-Position-MoveToCategoryAbove") };
            moveToCategoryAboveItem.Click += MoveToCategoryAboveItem_Click;

            // Move to category below
            MenuFlyoutItem moveToCategoryBelowItem = new MenuFlyoutItem { Text = loader.GetString("CategoryOptionsFlyout-Position-MoveToCategoryBelow") };
            moveToCategoryBelowItem.Click += MoveToCategoryBelowItem_Click;

            // Move to parent category
            MenuFlyoutItem moveToParentCategoryItem = new MenuFlyoutItem { Text = loader.GetString("CategoryOptionsFlyout-Position-MoveToParentCategory") };

            if (!isFirstItem)
            {
                // Move up
                moveUpFlyoutItem.Icon = arrowTop;
                positionSubFlyoutItem.Items.Add(moveUpFlyoutItem);
            }

            if(!isLastItem)
            {
                // Move down
                moveDownFlyoutItem.Icon = arrowBottom;
                positionSubFlyoutItem.Items.Add(moveDownFlyoutItem);
            }

            if(isFirstItem && !isLastItem && isSubCategory)
            {
                // Move to parent category
                moveToParentCategoryItem.Icon = arrowTopLeft;
                moveToParentCategoryItem.Click += MoveToParentCategoryAboveItem_Click;
                positionSubFlyoutItem.Items.Add(moveToParentCategoryItem);
            }

            if (!isFirstItem)
            {
                // Move to category above
                moveToCategoryAboveItem.Icon = arrowTopRight;
                positionSubFlyoutItem.Items.Add(moveToCategoryAboveItem);
            }

            if (!isLastItem)
            {
                // Move to category below
                moveToCategoryBelowItem.Icon = arrowBottomRight;
                positionSubFlyoutItem.Items.Add(moveToCategoryBelowItem);
            }

            if(isLastItem && isSubCategory)
            {
                // Move to parent category
                moveToParentCategoryItem.Icon = arrowBottomLeft;
                moveToParentCategoryItem.Click += MoveToParentCategoryBelowItem_Click;
                positionSubFlyoutItem.Items.Add(moveToParentCategoryItem);
            }

            if(positionSubFlyoutItem.Items.Count > 0)
                flyout.Items.Add(positionSubFlyoutItem);

            // Create subcategory
            MenuFlyoutItem createSubCategoryFlyoutItem = new MenuFlyoutItem { Text = loader.GetString("CategoryOptionsFlyout-AddSubCategory") };
            createSubCategoryFlyoutItem.Click += CreateSubCategoryFlyoutItem_Click;
            flyout.Items.Add(createSubCategoryFlyoutItem);

            flyout.ShowAt(sender, position);
        }

        #region Position
        private async void MoveUpFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            MoveCategoryMenuItem(SideBar.MenuItems, selectedCategory, true);
            await FileManager.MoveCategoryAndSaveOrderAsync(FileManager.itemViewHolder.Categories, selectedCategory, Guid.Empty, true);
        }

        private async void MoveDownFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            MoveCategoryMenuItem(SideBar.MenuItems, selectedCategory, false);
            await FileManager.MoveCategoryAndSaveOrderAsync(FileManager.itemViewHolder.Categories, selectedCategory, Guid.Empty, false);
        }

        private async void MoveToCategoryAboveItem_Click(object sender, RoutedEventArgs e)
        {
            MoveCategoryMenuItemToMenuItem(SideBar.MenuItems, selectedCategory, true);
            await FileManager.MoveCategoryToCategoryAndSaveOrderAsync(FileManager.itemViewHolder.Categories, selectedCategory, true);
        }

        private async void MoveToCategoryBelowItem_Click(object sender, RoutedEventArgs e)
        {
            MoveCategoryMenuItemToMenuItem(SideBar.MenuItems, selectedCategory, false);
            await FileManager.MoveCategoryToCategoryAndSaveOrderAsync(FileManager.itemViewHolder.Categories, selectedCategory, false);
        }

        private async void MoveToParentCategoryAboveItem_Click(object sender, RoutedEventArgs e)
        {
            MoveCategoryMenuItemToParent(SideBar.MenuItems, selectedCategory, true);
            await FileManager.MoveCategoryToParentAndSaveOrderAsync(FileManager.itemViewHolder.Categories, Guid.Empty, selectedCategory, true);
        }

        private async void MoveToParentCategoryBelowItem_Click(object sender, RoutedEventArgs e)
        {
            MoveCategoryMenuItemToParent(SideBar.MenuItems, selectedCategory, false);
            await FileManager.MoveCategoryToParentAndSaveOrderAsync(FileManager.itemViewHolder.Categories, Guid.Empty, selectedCategory, false);
        }
        #endregion

        #region Create SubCategory
        private async void CreateSubCategoryFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            var newSubCategoryContentDialog = ContentDialogs.CreateNewCategoryContentDialog(selectedCategory);
            newSubCategoryContentDialog.PrimaryButtonClick += NewSubCategoryContentDialog_PrimaryButtonClick;
            await newSubCategoryContentDialog.ShowAsync();
        }

        private async void NewSubCategoryContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Get combobox value
            ComboBoxItem typeItem = (ComboBoxItem)ContentDialogs.IconSelectionComboBox.SelectedItem;
            string icon = typeItem.Content.ToString();

            Guid categoryUuid = await FileManager.CreateCategoryAsync(null, selectedCategory, ContentDialogs.NewCategoryTextBox.Text, icon);
            Category newCategory = await FileManager.GetCategoryAsync(categoryUuid, false);

            // Add the category to the categories list
            FileManager.AddCategory(newCategory, selectedCategory);
            FileManager.itemViewHolder.TriggerCategoriesUpdatedEvent();

            // Add the category to the MenuItems of the SideBar
            AddCategoryMenuItem(SideBar.MenuItems, newCategory, selectedCategory);

            // Navigate to the new category
            await FileManager.ShowCategoryAsync(selectedCategory);
        }
        #endregion
        #endregion

        #region Export
        private async void MoreButton_ExportFlyout_Click(object sender, RoutedEventArgs e)
        {
            if (!await DownloadSelectedFiles()) return;

            List<Sound> sounds = new List<Sound>();
            foreach (var sound in FileManager.itemViewHolder.SelectedSounds)
                sounds.Add(sound);

            var template = (DataTemplate)Resources["SoundItemTemplate"];
            var listViewItemStyle = Resources["ListViewItemStyle"] as Style;
            var buttonRevealStyle = Resources["ButtonRevealStyle"] as Style;

            var exportSoundsContentDialog = ContentDialogs.CreateExportSoundsContentDialog(sounds, template, listViewItemStyle, buttonRevealStyle);
            exportSoundsContentDialog.PrimaryButtonClick += ExportSoundsContentDialog_PrimaryButtonClick;
            await exportSoundsContentDialog.ShowAsync();
        }

        private async void ExportSoundsContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            await FileManager.ExportSoundsAsync(ContentDialogs.SoundsList.ToList(), ContentDialogs.ExportSoundsAsZipCheckBox.IsChecked.Value, ContentDialogs.ExportSoundsFolder);
        }
        #endregion

        #region Delete Sounds
        private async void DeleteSoundsButton_Click(object sender, RoutedEventArgs e)
        {
            var deleteSoundsContentDialog = ContentDialogs.CreateDeleteSoundsContentDialogAsync();
            deleteSoundsContentDialog.PrimaryButtonClick += DeleteSoundsContentDialog_PrimaryButtonClick;
            await deleteSoundsContentDialog.ShowAsync();
        }

        private async void DeleteSoundsContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            FileManager.itemViewHolder.LoadingScreenMessage = loader.GetString("DeleteSoundsMessage");
            FileManager.itemViewHolder.LoadingScreenVisible = true;

            // Delete Sounds
            List<Guid> soundUuids = new List<Guid>();
            for (int i = 0; i < FileManager.itemViewHolder.SelectedSounds.Count; i++)
                soundUuids.Add(FileManager.itemViewHolder.SelectedSounds.ElementAt(i).Uuid);

            if (soundUuids.Count != 0)
            {
                await FileManager.DeleteSoundsAsync(soundUuids);

                // Remove deleted sounds from the lists
                foreach (var soundUuid in soundUuids)
                    FileManager.RemoveSound(soundUuid);
            }

            // Clear selected sounds list
            FileManager.itemViewHolder.SelectedSounds.Clear();
            FileManager.itemViewHolder.LoadingScreenVisible = false;
        }
        #endregion

        private void SelectCategory(Guid categoryUuid)
        {
            foreach (WinUI.NavigationViewItem item in SideBar.MenuItems)
            {
                if ((Guid)item.Tag == categoryUuid)
                {
                    item.IsSelected = true;
                    return;
                }
            }
        }
    }
}