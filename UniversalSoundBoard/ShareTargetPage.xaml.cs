using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UniversalSoundBoard.Model;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Leere Seite" wird unter https://go.microsoft.com/fwlink/?LinkId=234238 dokumentiert.

namespace UniversalSoundBoard
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet oder zu der innerhalb eines Rahmens navigiert werden kann.
    /// </summary>
    public sealed partial class ShareTargetPage : Page
    {
        ObservableCollection<Category> categories;
        ShareOperation shareOperation;
        List<StorageFile> items;

        public ShareTargetPage()
        {
            this.InitializeComponent();
            this.DataContextChanged += (s, e) => Bindings.Update();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            shareOperation = e.Parameter as ShareOperation;

            items = new List<StorageFile>();

            foreach (StorageFile file in await shareOperation.Data.GetStorageItemsAsync())
            {
                items.Add(file);
            }
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            categories = new ObservableCollection<Category>();
            categories.Add(new Category((new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("AllSounds"), "\uE10F"));

            // Get all Categories and show them
            foreach(Category cat in await FileManager.GetCategoriesListAsync())
            {
                categories.Add(cat);
            }
            Bindings.Update();
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
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
                        Sound sound = new Sound(storagefile.DisplayName, category, storagefile as StorageFile);
                        await FileManager.addSound(sound);
                    }
                }
            }
            shareOperation.ReportCompleted();
        }

        private void CategoriesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AddButton.IsEnabled = true;
        }
    }
}
