using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
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

        public MainPage()
        {
            this.InitializeComponent();
            Loaded += MainPage_Loaded;
            SystemNavigationManager.GetForCurrentView().BackRequested += onBackRequested;

            BackButton.Visibility = Visibility.Collapsed;
            Suggestions = new List<string>();
        }

        async void MainPage_Loaded(object sender, RoutedEventArgs e)
        {
            setDataContext();
            await SoundManager.GetAllSounds();
            HomeListBoxItem.IsSelected = true;
        }

        private void setDataContext()
        {
            ContentRoot.DataContext = (App.Current as App)._itemViewHolder;
        }

        private void HamburgerButton_Click(object sender, RoutedEventArgs e)
        {
            SideBar.IsPaneOpen = !SideBar.IsPaneOpen;
        }

        private async void SearchAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (String.IsNullOrEmpty(sender.Text)) goBack();

            (App.Current as App)._itemViewHolder.title = sender.Text;
            await SoundManager.GetSoundsByName(sender.Text);
            Suggestions = (App.Current as App)._itemViewHolder.sounds.Where(p => p.Name.StartsWith(sender.Text)).Select(p => p.Name).ToList();
            SearchAutoSuggestBox.ItemsSource = Suggestions;
            BackButton.Visibility = Visibility.Visible;
        }

        private async void SearchAutoSuggestBox_TextChanged_Mobile(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (String.IsNullOrEmpty(sender.Text)) goBack();

            (App.Current as App)._itemViewHolder.title = sender.Text;
            await SoundManager.GetSoundsByName(sender.Text);
            Suggestions = (App.Current as App)._itemViewHolder.sounds.Where(p => p.Name.StartsWith(sender.Text)).Select(p => p.Name).ToList();
            SearchAutoSuggestBox.ItemsSource = Suggestions;
            BackButton.Visibility = Visibility.Visible;
        }

        private async void SearchAutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            await SoundManager.GetSoundsByName(sender.Text);
            (App.Current as App)._itemViewHolder.title = sender.Text;
        }

        private async void SearchAutoSuggestBox_QuerySubmitted_Mobile(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            await SoundManager.GetSoundsByName(sender.Text);
            (App.Current as App)._itemViewHolder.title = sender.Text;
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
            MyMediaElement.Source = new Uri(this.BaseUri, sound.AudioFile);
        }

        private async void SoundGridView_Drop(object sender, DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();

                if (items.Any())
                {
                    var storageFile = items[0] as StorageFile;
                    var contentType = storageFile.ContentType;

                    StorageFolder folder = ApplicationData.Current.LocalFolder;

                    if (contentType == "audio/wav" || contentType == "audio/mpeg")
                    {
                        addSound(storageFile);
                    }
                }
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
            HomeListBoxItem.IsSelected = true;
            BackButton.Visibility = Visibility.Collapsed;
            SearchAutoSuggestBox.Text = "";
        }

        private async void goBack_Mobile()
        {
            await SoundManager.GetAllSounds();
            (App.Current as App)._itemViewHolder.title = "All Sounds";
            HomeListBoxItem.IsSelected = true;

            SearchAutoSuggestBox.Text = "";
            SearchAutoSuggestBox.Visibility = Visibility.Collapsed;

            BackButton.Visibility = Visibility.Collapsed;
            AddButton.Visibility = Visibility.Visible;
            SearchButton.Visibility = Visibility.Visible;
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

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            // Open file explorer
            var picker = new Windows.Storage.Pickers.FileOpenPicker();
            picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.Thumbnail;
            picker.SuggestedStartLocation =
                Windows.Storage.Pickers.PickerLocationId.MusicLibrary;
            picker.FileTypeFilter.Add(".mp3");
            picker.FileTypeFilter.Add(".wav");

            var files = await picker.PickMultipleFilesAsync();
            if (files.Count > 0)
            {
                StringBuilder output = new StringBuilder("Picked files:\n");

                // Application now has read/write access to the picked file(s)
                foreach (StorageFile sound in files)
                {
                    output.Append(sound.Name + "\n");
                    addSound(sound);
                }
            }
        }

        private async void addSound(StorageFile file)
        {
            StorageFolder folder = ApplicationData.Current.LocalFolder;

            StorageFile newFile = await file.CopyAsync(folder, file.Name, NameCollisionOption.GenerateUniqueName);

            MyMediaElement.SetSource(await file.OpenAsync(FileAccessMode.Read), file.ContentType);
            MyMediaElement.Play();

            (App.Current as App)._itemViewHolder.sounds.Add(new Sound(newFile.Name, SoundCategory.Warnings, newFile.Path));
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
    }
}
