using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UniversalSoundBoard.Model;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using static UniversalSoundBoard.Model.Sound;

// Die Elementvorlage "Leere Seite" ist unter http://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace UniversalSoundBoard
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class SoundPage : Page
    {
   
        public SoundPage()
        {
            this.InitializeComponent();
            Loaded += SoundPage_Loaded;
        }

        void SoundPage_Loaded(object sender, RoutedEventArgs e)
        {
            setDataContext();
        }

        private void setDataContext()
        {
            ContentRoot.DataContext = (App.Current as App)._itemViewHolder;
        }
        
        private void SoundGridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var sound = (Sound)e.ClickedItem;
            //MyMediaElement.Source = new Uri(this.BaseUri, sound.AudioFile.Path);
            if ((App.Current as App)._itemViewHolder.selectionMode == ListViewSelectionMode.None)
            {
                (App.Current as App)._itemViewHolder.mediaElementSource = new Uri(this.BaseUri, sound.AudioFile.Path);
            }
        }

        private async void SoundGridView_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();

                (App.Current as App)._itemViewHolder.progressRingIsActive = true;
                if (items.Any()){
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
                                await SoundManager.addSound(storageFile);
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
            e.DragUIOverride.Caption = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("Drop");
            e.DragUIOverride.IsCaptionVisible = true;
            e.DragUIOverride.IsContentVisible = true;
            e.DragUIOverride.IsGlyphVisible = true;
            //e.DragUIOverride.SetContentFromBitmapImage(new BitmapImage(new Uri("ms-appx:///Assets/clippy.jpg")));
        }

        private void SoundGridView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            //Sound item = (sender as GridView).DataContext as Sound;
            //GridViewItem item = (sender as GridView).DataContext as GridViewItem;
            switchSelectionMode();
        }

        private void SoundGridView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            //GridViewItem item = (sender as GridView).DataContext as GridViewItem;
            switchSelectionMode();
        }

        private void SoundGridView_Holding(object sender, HoldingRoutedEventArgs e)
        {
            //GridViewItem item = (sender as GridView).DataContext as GridViewItem;
            switchSelectionMode();
        }

        private void switchSelectionMode()
        {
            if ((App.Current as App)._itemViewHolder.selectionMode == ListViewSelectionMode.Multiple)
            {
                // TODO: Select tapped item programmatically
                //item.IsSelected = true;
            }
            else
            {
                (App.Current as App)._itemViewHolder.selectionMode = ListViewSelectionMode.Multiple;
                (App.Current as App)._itemViewHolder.normalOptionsVisibility = Visibility.Collapsed;
                (App.Current as App)._itemViewHolder.multiSelectOptionsVisibility = Visibility.Visible;

                //SoundGridView.SelectedItems.Add(item);
            }
        }

        private void SoundGridView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // If no items are selected, disable multi select buttons
            if (SoundGridView.SelectedItems.Count > 0)
            {
                (App.Current as App)._itemViewHolder.multiSelectOptionsEnabled = true;
            }
            else
            {
                (App.Current as App)._itemViewHolder.multiSelectOptionsEnabled = false;
            }

            // Add new item to selectedSounds list
            if(e.AddedItems.Count == 1)
            {
                (App.Current as App)._itemViewHolder.selectedSounds.Add((Sound)e.AddedItems.First());
            }else
            {
                (App.Current as App)._itemViewHolder.selectedSounds.Remove((Sound)e.RemovedItems.First());
            }
        } 


        // Content Dialog Methods
        private async void CategoryDeleteButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var deleteCategoryContentDialog = ContentDialogs.CreateDeleteCategoryContentDialogAsync();
            deleteCategoryContentDialog.PrimaryButtonClick += DeleteCategoryContentDialog_PrimaryButtonClick;
            await deleteCategoryContentDialog.ShowAsync();
        }

        private async void DeleteCategoryContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            await FileManager.deleteCategory((App.Current as App)._itemViewHolder.title);
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
            (App.Current as App)._itemViewHolder.title = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("AllSounds");
            (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;

            // Reload page
            this.Frame.Navigate(this.GetType());
        }

        private async void CategoryEditButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            var editCategoryContentDialog = await ContentDialogs.CreateEditCategoryContentDialogAsync();
            editCategoryContentDialog.PrimaryButtonClick += EditCategoryContentDialog_PrimaryButtonClick;
            await editCategoryContentDialog.ShowAsync();
        }

        private async void EditCategoryContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Get categories List and save with new value
            List<Category> categoriesList = await FileManager.GetCategoriesListAsync();

            // Get combobox value
            ComboBoxItem typeItem = (ComboBoxItem)ContentDialogs.IconSelectionComboBox.SelectedItem;
            string icon = typeItem.Content.ToString();

            string newName = ContentDialogs.EditCategoryTextBox.Text;
            string oldName = (App.Current as App)._itemViewHolder.title;

            categoriesList.Find(p => p.Name == oldName).Icon = icon;
            categoriesList.Find(p => p.Name == oldName).Name = newName;

            await FileManager.SaveCategoriesListAsync(categoriesList);
            await FileManager.renameCategory(oldName, newName);

            // Reload page
            this.Frame.Navigate(this.GetType());
            (App.Current as App)._itemViewHolder.title = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("AllSounds");
            (App.Current as App)._itemViewHolder.editButtonVisibility = Visibility.Collapsed;
        }
    }
}
