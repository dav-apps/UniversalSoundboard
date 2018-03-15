using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using UniversalSoundBoard.DataAccess;
using UniversalSoundBoard.Models;
using UniversalSoundBoard.Pages;
using Windows.Media.Playback;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace UniversalSoundBoard.Components
{
    public sealed partial class PlayingSoundTemplate : UserControl
    {
        public PlayingSound PlayingSound { get; set; }

        CoreDispatcher dispatcher;
        public string FavouriteFlyoutText = "Add Favorite";

        
        public PlayingSoundTemplate()
        {
            InitializeComponent();
            Loaded += PlayingSoundTemplate_Loaded;

            SetDarkThemeLayout();
            SetDataContext();
            DataContextChanged += PlayingSoundTemplate_DataContextChanged;

            dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
        }
        
        private void PlayingSoundTemplate_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if(DataContext != null)
            {
                PlayingSound = DataContext as PlayingSound;
                InitializePlayingSound();
            }
        }
        
        private void PlayingSoundTemplate_Loaded(object sender, RoutedEventArgs eventArgs)
        {
            InitializePlayingSound();
            SetMediaPlayerElementIsCompact();
        }
        
        private void SetDataContext()
        {
            ContentRoot.DataContext = (App.Current as App)._itemViewHolder;
        }
        
        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SetMediaPlayerElementIsCompact();
        }
        
        private void SetMediaPlayerElementIsCompact()
        {
            if(Window.Current.Bounds.Width < FileManager.mobileMaxWidth)
            {
                MediaPlayerElement.TransportControls.IsCompact = true;
            }else
            {
                MediaPlayerElement.TransportControls.IsCompact = false;
            }
        }
        
        private void SetDarkThemeLayout()
        {
            if((App.Current as App).RequestedTheme == ApplicationTheme.Dark)
            {
                ContentRoot.Background = new SolidColorBrush(Colors.Black);
            }
        }
        
        private void RepeatSound(int repetitions)
        {
            PlayingSound.Repetitions = ++repetitions;
            FileManager.SetRepetitionsOfPlayingSound(PlayingSound.Uuid, ++repetitions);
        }
        
        private void InitializePlayingSound()
        {
            if (PlayingSound.MediaPlayer != null)
            {
                MediaPlayerElement.SetMediaPlayer(PlayingSound.MediaPlayer);
                MediaPlayerElement.MediaPlayer.MediaEnded -= Player_MediaEnded;
                MediaPlayerElement.MediaPlayer.MediaEnded += Player_MediaEnded;
                ((MediaPlaybackList)PlayingSound.MediaPlayer.Source).CurrentItemChanged -= PlayingSoundTemplate_CurrentItemChanged;
                ((MediaPlaybackList)PlayingSound.MediaPlayer.Source).CurrentItemChanged += PlayingSoundTemplate_CurrentItemChanged;
                PlayingSoundName.Text = PlayingSound.CurrentSound.Name;

                // Set the text of the add to Favourites Flyout
                FrameworkElement transportControlsTemplateRoot = (FrameworkElement)VisualTreeHelper.GetChild(MediaPlayerElement.TransportControls, 0);
                AppBarButton FavouriteFlyout = (AppBarButton)transportControlsTemplateRoot.FindName("FavouriteFlyout");
                FavouriteFlyout.Label = PlayingSound.CurrentSound.Favourite ?
                    FavouriteFlyout.Label = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("SoundTile-UnsetFavourite") :
                    FavouriteFlyout.Label = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("SoundTile-SetFavourite");
            }
        }
        
        private void RemovePlayingSound()
        {
            if (PlayingSound.MediaPlayer != null)
            {
                PlayingSound.MediaPlayer.Pause();
                MediaPlayerElement.MediaPlayer.MediaEnded -= Player_MediaEnded;
                ((MediaPlaybackList)PlayingSound.MediaPlayer.Source).CurrentItemChanged -= PlayingSoundTemplate_CurrentItemChanged;
                MediaPlayerElement.SetMediaPlayer(null);
                PlayingSoundName.Text = "";
                PlayingSound.MediaPlayer = null;
            }
            SoundPage.RemovePlayingSound(PlayingSound);
        }
        
        private async void Player_MediaEnded(MediaPlayer sender, object args)
        {
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                PlayingSound.Repetitions--;
                FileManager.SetRepetitionsOfPlayingSound(PlayingSound.Uuid, PlayingSound.Repetitions);
                if (PlayingSound.Repetitions <= 0)
                {
                    RemovePlayingSound();
                }
                
                if(PlayingSound.Repetitions >= 0 && PlayingSound.MediaPlayer != null)
                {
                    if (PlayingSound.Sounds.Count > 1) // Multiple Sounds in the list
                    {
                        // If randomly is true, shuffle sounds
                        if (PlayingSound.Randomly)
                        {
                            Random random = new Random();

                            // Copy old lists and use them to be able to remove entries
                            MediaPlaybackList oldMediaPlaybackList = new MediaPlaybackList();
                            List<Sound> oldSoundsList = new List<Sound>();

                            foreach (var item in ((MediaPlaybackList)PlayingSound.MediaPlayer.Source).Items)
                                oldMediaPlaybackList.Items.Add(item);
                            foreach (var item in PlayingSound.Sounds)
                                oldSoundsList.Add(item);

                            MediaPlaybackList newMediaPlaybackList = new MediaPlaybackList();
                            List<Sound> newSoundsList = new List<Sound>();

                            // Add items to new lists in random order
                            for (int i = 0; i < PlayingSound.Sounds.Count; i++)
                            {
                                int randomNumber = random.Next(oldSoundsList.Count);
                                newSoundsList.Add(oldSoundsList.ElementAt(randomNumber));
                                newMediaPlaybackList.Items.Add(oldMediaPlaybackList.Items.ElementAt(randomNumber));

                                oldSoundsList.RemoveAt(randomNumber);
                                oldMediaPlaybackList.Items.RemoveAt(randomNumber);
                            }

                            // Replace the old lists with the new ones
                            ((MediaPlaybackList)PlayingSound.MediaPlayer.Source).Items.Clear();
                            foreach (var item in newMediaPlaybackList.Items)
                                ((MediaPlaybackList)PlayingSound.MediaPlayer.Source).Items.Add(item);

                            PlayingSound.Sounds.Clear();
                            foreach (var item in newSoundsList)
                                PlayingSound.Sounds.Add(item);

                            // Update PlayingSound in the Database
                            FileManager.SetSoundsListOfPlayingSound(PlayingSound.Uuid, newSoundsList);
                        }

                        ((MediaPlaybackList)PlayingSound.MediaPlayer.Source).MoveTo(0);
                        FileManager.SetCurrentOfPlayingSound(PlayingSound.Uuid, 0);
                    }
                    PlayingSound.MediaPlayer.Play();
                }
            });
        }
        
        private async void PlayingSoundTemplate_CurrentItemChanged(MediaPlaybackList sender, CurrentMediaPlaybackItemChangedEventArgs args)
        {
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if(PlayingSound.Sounds.Count > 1 && sender.CurrentItemIndex < PlayingSound.Sounds.Count)
                {
                    int currentItemIndex = (int)sender.CurrentItemIndex;
                    PlayingSound.CurrentSound = PlayingSound.Sounds.ElementAt(currentItemIndex);
                    FileManager.SetCurrentOfPlayingSound(PlayingSound.Uuid, currentItemIndex);
                    PlayingSoundName.Text = PlayingSound.CurrentSound.Name;

                    // Set the text of the add to Favourites Flyout
                    FrameworkElement transportControlsTemplateRoot = (FrameworkElement)VisualTreeHelper.GetChild(MediaPlayerElement.TransportControls, 0);
                    AppBarButton FavouriteFlyout = (AppBarButton)transportControlsTemplateRoot.FindName("FavouriteFlyout");
                    FavouriteFlyout.Label = PlayingSound.CurrentSound.Favourite ?
                        (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("SoundTile-UnsetFavourite") :
                        (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("SoundTile-SetFavourite");
                }
            });
        }
        
        private void CustomMediaTransportControls_Removed(object sender, EventArgs e)
        {
            RemovePlayingSound();
        }
        
        private void CustomMediaTransportControls_FavouriteFlyout_Clicked(object sender, EventArgs e)
        {
            bool oldFav = PlayingSound.CurrentSound.Favourite;
            bool newFav = !PlayingSound.CurrentSound.Favourite;
            Sound currentSound = PlayingSound.CurrentSound;

            // Update all lists containing sounds with the new favourite value
            List<ObservableCollection<Sound>> soundLists = new List<ObservableCollection<Sound>>
            {
                (App.Current as App)._itemViewHolder.sounds,
                (App.Current as App)._itemViewHolder.allSounds,
                (App.Current as App)._itemViewHolder.favouriteSounds
            };

            foreach (ObservableCollection<Sound> soundList in soundLists)
            {
                var sounds = soundList.Where(s => s.Uuid == currentSound.Uuid);
                if (sounds.Count() > 0)
                {
                    sounds.First().Favourite = newFav;
                }
            }

            
            // Set the text of the Add to Favourites Flyout
            FrameworkElement transportControlsTemplateRoot = (FrameworkElement)VisualTreeHelper.GetChild(MediaPlayerElement.TransportControls, 0);
            AppBarButton FavouriteFlyout = (AppBarButton)transportControlsTemplateRoot.FindName("FavouriteFlyout");

            if (oldFav)
            {
                // Remove sound from favourites
                (App.Current as App)._itemViewHolder.favouriteSounds.Remove(currentSound);
                FavouriteFlyout.Label = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("SoundTile-SetFavourite");
            }
            else
            {
                // Add to favourites
                (App.Current as App)._itemViewHolder.favouriteSounds.Add(currentSound);
                FavouriteFlyout.Label = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("SoundTile-UnsetFavourite");
            }

            FileManager.SetSoundAsFavourite(currentSound.Uuid, newFav);
        }
        
        private void CustomMediaTransportControls_Repeat_1x_Clicked(object sender, EventArgs e)
        {
            RepeatSound(1);
        }
        
        private void CustomMediaTransportControls_Repeat_2x_Clicked(object sender, EventArgs e)
        {
            RepeatSound(2);
        }
        
        private void CustomMediaTransportControls_Repeat_5x_Clicked(object sender, EventArgs e)
        {
            RepeatSound(5);
        }
        
        private void CustomMediaTransportControls_Repeat_10x_Clicked(object sender, EventArgs e)
        {
            RepeatSound(10);
        }
        
        private void CustomMediaTransportControls_Repeat_endless_Clicked(object sender, EventArgs e)
        {
            RepeatSound(int.MaxValue);
        }
    }
}
