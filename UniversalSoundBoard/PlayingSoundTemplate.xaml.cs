using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UniversalSoundBoard.Model;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.Playback;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace UniversalSoundBoard
{
    public sealed partial class PlayingSoundTemplate : UserControl
    {
        public PlayingSound PlayingSound { get; set; }

        CoreDispatcher dispatcher;
        public string FavouriteFlyoutText = "Add Favorite";

        public PlayingSoundTemplate()
        {
            this.InitializeComponent();
            Loaded += PlayingSoundTemplate_Loaded;

            setDarkThemeLayout();
            setDataContext();
            DataContextChanged += PlayingSoundTemplate_DataContextChanged;

            dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
        }

        private void PlayingSoundTemplate_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if(this.DataContext != null)
            {
                this.PlayingSound = this.DataContext as PlayingSound;

                initializePlayingSound();
            }
        }

        private void PlayingSoundTemplate_Loaded(object sender, RoutedEventArgs eventArgs)
        {
            initializePlayingSound();
            setMediaPlayerElementIsCompact();
        }

        private void setDataContext()
        {
            ContentRoot.DataContext = (App.Current as App)._itemViewHolder;
        }

        private void setMediaPlayerElementIsCompact()
        {
            if(Window.Current.Bounds.Width < FileManager.mobileMaxWidth)
            {
                MediaPlayerElement.TransportControls.IsCompact = true;
            }else
            {
                MediaPlayerElement.TransportControls.IsCompact = false;
            }
        }

        private void setDarkThemeLayout()
        {
            if((App.Current as App).RequestedTheme == ApplicationTheme.Dark)
            {
                ContentRoot.Background = new SolidColorBrush(Colors.Black);
            }
        }

        private void repeatSound(int repetitions)
        {
            this.PlayingSound.repetitions = repetitions;
        }

        private void initializePlayingSound()
        {
            if (this.PlayingSound.MediaPlayer != null)
            {
                MediaPlayerElement.SetMediaPlayer(this.PlayingSound.MediaPlayer);
                MediaPlayerElement.MediaPlayer.MediaEnded -= Player_MediaEnded;
                MediaPlayerElement.MediaPlayer.MediaEnded += Player_MediaEnded;
                ((MediaPlaybackList)PlayingSound.MediaPlayer.Source).CurrentItemChanged -= PlayingSoundTemplate_CurrentItemChanged;
                ((MediaPlaybackList)PlayingSound.MediaPlayer.Source).CurrentItemChanged += PlayingSoundTemplate_CurrentItemChanged;
                PlayingSoundName.Text = this.PlayingSound.CurrentSound.Name;
                if(this.PlayingSound.repetitions >= 0)
                {
                    MediaPlayerElement.MediaPlayer.Play();
                }

                // Set the text of the add to Favourites Flyout
                FrameworkElement transportControlsTemplateRoot = (FrameworkElement)VisualTreeHelper.GetChild(MediaPlayerElement.TransportControls, 0);
                AppBarButton FavouriteFlyout = (AppBarButton)transportControlsTemplateRoot.FindName("FavouriteFlyout");
                if (this.PlayingSound.CurrentSound.Favourite)
                {
                    FavouriteFlyout.Label = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("SoundTile-UnsetFavourite");
                }
                else
                {
                    FavouriteFlyout.Label = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("SoundTile-SetFavourite");
                }
            }
        }

        private void removePlayingSound()
        {
            if (this.PlayingSound.MediaPlayer != null)
            {
                this.PlayingSound.MediaPlayer.Pause();
                MediaPlayerElement.MediaPlayer.MediaEnded -= Player_MediaEnded;
                ((MediaPlaybackList)PlayingSound.MediaPlayer.Source).CurrentItemChanged -= PlayingSoundTemplate_CurrentItemChanged;
                MediaPlayerElement.SetMediaPlayer(null);
                PlayingSoundName.Text = "";
                this.PlayingSound.MediaPlayer = null;
            }
            SoundPage.RemovePlayingSound(this.PlayingSound);
        }

        private async void Player_MediaEnded(MediaPlayer sender, object args)
        {
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                this.PlayingSound.repetitions--;
                if (this.PlayingSound.repetitions <= 0)
                {
                    removePlayingSound();
                }
                
                if(this.PlayingSound.repetitions >= 0 && this.PlayingSound.MediaPlayer != null)
                {
                    if (this.PlayingSound.Sounds.Count > 1) // Multiple Sounds in the list
                    {
                        // If randomly is true, shuffle sounds
                        if (this.PlayingSound.randomly)
                        {
                            Random random = new Random();
                            this.PlayingSound.Sounds = this.PlayingSound.Sounds.OrderBy(a => random.Next()).ToList();
                        }

                        ((MediaPlaybackList)this.PlayingSound.MediaPlayer.Source).MoveTo(0);
                    }
                    this.PlayingSound.MediaPlayer.Play();
                }
            });
        }

        private async void PlayingSoundTemplate_CurrentItemChanged(MediaPlaybackList sender, CurrentMediaPlaybackItemChangedEventArgs args)
        {
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                if(this.PlayingSound.Sounds.Count > 1 && sender.CurrentItemIndex < this.PlayingSound.Sounds.Count)
                {
                    this.PlayingSound.CurrentSound = this.PlayingSound.Sounds.ElementAt((int)sender.CurrentItemIndex);
                    PlayingSoundName.Text = this.PlayingSound.CurrentSound.Name;

                    // Set the text of the add to Favourites Flyout
                    FrameworkElement transportControlsTemplateRoot = (FrameworkElement)VisualTreeHelper.GetChild(MediaPlayerElement.TransportControls, 0);
                    AppBarButton FavouriteFlyout = (AppBarButton)transportControlsTemplateRoot.FindName("FavouriteFlyout");
                    if (this.PlayingSound.CurrentSound.Favourite)
                    {
                        FavouriteFlyout.Label = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("SoundTile-UnsetFavourite");
                    }
                    else
                    {
                        FavouriteFlyout.Label = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("SoundTile-SetFavourite");
                    }
                }
            });
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            setMediaPlayerElementIsCompact();
        }

        private void CustomMediaTransportControls_Removed(object sender, EventArgs e)
        {
            removePlayingSound();
        }

        private async void CustomMediaTransportControls_FavouriteFlyout_Clicked(object sender, EventArgs e)
        {
            bool oldFav = this.PlayingSound.CurrentSound.Favourite;
            bool newFav = !this.PlayingSound.CurrentSound.Favourite;
            Sound currentSound = this.PlayingSound.CurrentSound;

            // Update all lists containing sounds with the new favourite value
            List<ObservableCollection<Sound>> soundLists = new List<ObservableCollection<Sound>>();
            soundLists.Add((App.Current as App)._itemViewHolder.sounds);
            soundLists.Add((App.Current as App)._itemViewHolder.allSounds);
            soundLists.Add((App.Current as App)._itemViewHolder.favouriteSounds);

            foreach (ObservableCollection<Sound> soundList in soundLists)
            {
                var sounds = soundList.Where(s => s.Name == currentSound.Name);
                if (sounds.Count() > 0)
                {
                    sounds.First().Favourite = newFav;
                }
            }


            // Set the text of the add to Favourites Flyout
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

            await FileManager.setSoundAsFavourite(currentSound, newFav);
        }

        private void CustomMediaTransportControls_Repeat_1x_Clicked(object sender, EventArgs e)
        {
            repeatSound(1);
        }

        private void CustomMediaTransportControls_Repeat_2x_Clicked(object sender, EventArgs e)
        {
            repeatSound(2);
        }

        private void CustomMediaTransportControls_Repeat_5x_Clicked(object sender, EventArgs e)
        {
            repeatSound(5);
        }

        private void CustomMediaTransportControls_Repeat_10x_Clicked(object sender, EventArgs e)
        {
            repeatSound(10);
        }

        private void CustomMediaTransportControls_Repeat_endless_Clicked(object sender, EventArgs e)
        {
            repeatSound(int.MaxValue);
        }
    }
}
