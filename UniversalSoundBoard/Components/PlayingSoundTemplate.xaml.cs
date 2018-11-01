using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using UniversalSoundBoard.DataAccess;
using UniversalSoundBoard.Models;
using UniversalSoundBoard.Pages;
using Windows.Foundation;
using Windows.Media.Casting;
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
        private bool skipVolumeSliderValueChangedEvent = false;


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
            AdjustLayout();
        }
        
        private void SetDataContext()
        {
            ContentRoot.DataContext = (App.Current as App)._itemViewHolder;
        }
        
        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            AdjustLayout();
        }
        
        private void AdjustLayout()
        {
            MediaPlayerElement.TransportControls.IsCompact = Window.Current.Bounds.Width < FileManager.mobileMaxWidth;
            (MediaPlayerElement.TransportControls as CustomMediaTransportControls).SetVolumeButtonVisibility(Window.Current.Bounds.Width > FileManager.topButtonsCollapsedMaxWidth);
            MediaPlayerElement.TransportControls.IsVolumeButtonVisible = Window.Current.Bounds.Width > FileManager.topButtonsCollapsedMaxWidth;
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
            int newRepetitions = repetitions + 1;
            PlayingSound.Repetitions = newRepetitions;
            FileManager.SetRepetitionsOfPlayingSound(PlayingSound.Uuid, newRepetitions);
        }
        
        private void InitializePlayingSound()
        {
            if(PlayingSound != null)
            {
                if (PlayingSound.MediaPlayer != null)
                {
                    MediaPlayerElement.SetMediaPlayer(PlayingSound.MediaPlayer);
                    MediaPlayerElement.MediaPlayer.MediaEnded -= Player_MediaEnded;
                    MediaPlayerElement.MediaPlayer.MediaEnded += Player_MediaEnded;
                    PlayingSound.MediaPlayer.CommandManager.PreviousReceived += MediaPlayerCommandManager_PreviousReceived;
                    ((MediaPlaybackList)PlayingSound.MediaPlayer.Source).CurrentItemChanged -= PlayingSoundTemplate_CurrentItemChanged;
                    ((MediaPlaybackList)PlayingSound.MediaPlayer.Source).CurrentItemChanged += PlayingSoundTemplate_CurrentItemChanged;
                    PlayingSoundName.Text = PlayingSound.CurrentSound.Name;

                    // Set the text of the add to Favourites Flyout
                    FrameworkElement transportControlsTemplateRoot = (FrameworkElement)VisualTreeHelper.GetChild(MediaPlayerElement.TransportControls, 0);
                    MenuFlyoutItem FavouriteFlyout = (MenuFlyoutItem)transportControlsTemplateRoot.FindName("FavouriteMenuFlyoutItem");
                    FavouriteFlyout.Text = PlayingSound.CurrentSound.Favourite ?
                        FavouriteFlyout.Text = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("SoundTile-UnsetFavourite") :
                        FavouriteFlyout.Text = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("SoundTile-SetFavourite");

                    // Hide or show the Previous and Next buttons
                    int currentItemIndex = (int)((MediaPlaybackList)PlayingSound.MediaPlayer.Source).CurrentItemIndex;
                    MediaPlayerElement.TransportControls.IsPreviousTrackButtonVisible = currentItemIndex != 0;
                    MediaPlayerElement.TransportControls.IsNextTrackButtonVisible = currentItemIndex != PlayingSound.Sounds.Count - 1;

                    AdjustLayout();
                }
            }
        }

        private void MediaPlayerCommandManager_PreviousReceived(MediaPlaybackCommandManager sender, MediaPlaybackCommandManagerPreviousReceivedEventArgs args)
        {
            if(PlayingSound.MediaPlayer.PlaybackSession.Position.Seconds > 5)
            {
                args.Handled = true;

                // Move the state of the current sound to the beginning
                PlayingSound.MediaPlayer.PlaybackSession.Position = new TimeSpan(0);
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
                PlayingSound.MediaPlayer.SystemMediaTransportControls.IsEnabled = false;
                PlayingSound.MediaPlayer = null;
            }
            SoundPage.RemovePlayingSound(PlayingSound);
        }
        
        private async void Player_MediaEnded(MediaPlayer sender, object args)
        {
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                PlayingSound.Repetitions--;

                if (PlayingSound.Repetitions <= 0)
                    RemovePlayingSound();
                else
                    FileManager.SetRepetitionsOfPlayingSound(PlayingSound.Uuid, PlayingSound.Repetitions);

                if (PlayingSound.Repetitions >= 0 && PlayingSound.MediaPlayer != null)
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
                    MenuFlyoutItem FavouriteFlyout = (MenuFlyoutItem)transportControlsTemplateRoot.FindName("FavouriteMenuFlyoutItem");
                    FavouriteFlyout.Text = PlayingSound.CurrentSound.Favourite ?
                        (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("SoundTile-UnsetFavourite") :
                        (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("SoundTile-SetFavourite");

                    // Hide or show the Previous and Next buttons
                    MediaPlayerElement.TransportControls.IsPreviousTrackButtonVisible = currentItemIndex != 0;
                    MediaPlayerElement.TransportControls.IsNextTrackButtonVisible = currentItemIndex != PlayingSound.Sounds.Count - 1;
                }
            });
        }

        private void CustomMediaTransportControls_VolumeSlider_LostFocus(object sender, EventArgs e)
        {
            if (MediaPlayerElement.MediaPlayer != null)
            {
                // Save new Volume
                FileManager.SetVolumeOfPlayingSound(PlayingSound.Uuid, MediaPlayerElement.MediaPlayer.Volume);
            }
        }

        private void CustomMediaTransportControls_VolumeSlider_ValueChanged(object sender, Windows.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (skipVolumeSliderValueChangedEvent) return;
            double addedValue = e.NewValue - e.OldValue;

            if ((PlayingSound.MediaPlayer.Volume + addedValue / 100) > 1)
            {
                PlayingSound.MediaPlayer.Volume = 1;
            }
            else if ((PlayingSound.MediaPlayer.Volume + addedValue / 100) < 0)
            {
                PlayingSound.MediaPlayer.Volume = 0;
            }
            else
            {
                PlayingSound.MediaPlayer.Volume += addedValue / 100;
            }
        }

        private void CustomMediaTransportControls_RemoveButton_Clicked(object sender, EventArgs e)
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
                (App.Current as App)._itemViewHolder.Sounds,
                (App.Current as App)._itemViewHolder.AllSounds,
                (App.Current as App)._itemViewHolder.FavouriteSounds
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
            MenuFlyoutItem FavouriteFlyout = (MenuFlyoutItem)transportControlsTemplateRoot.FindName("FavouriteMenuFlyoutItem");

            if (oldFav)
            {
                // Remove sound from favourites
                (App.Current as App)._itemViewHolder.FavouriteSounds.Remove(currentSound);
                FavouriteFlyout.Text = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("SoundTile-SetFavourite");
            }
            else
            {
                // Add to favourites
                (App.Current as App)._itemViewHolder.FavouriteSounds.Add(currentSound);
                FavouriteFlyout.Text = (new Windows.ApplicationModel.Resources.ResourceLoader()).GetString("SoundTile-UnsetFavourite");
            }

            FileManager.SetSoundAsFavourite(currentSound.Uuid, newFav);
        }

        private void CustomMediaTransportControls_CastButton_Clicked(object sender, EventArgs e)
        {
            MenuFlyoutItem castButton = sender as MenuFlyoutItem;
            GeneralTransform transform = castButton.TransformToVisual(Window.Current.Content as UIElement);
            Point pt = transform.TransformPoint(new Point(0, 0));

            CastingDevicePicker castingPicker = new CastingDevicePicker();
            castingPicker.CastingDeviceSelected += CastingPicker_CastingDeviceSelected;
            castingPicker.Show(new Rect(pt.X, pt.Y, castButton.ActualWidth, castButton.ActualHeight));
        }

        private async void CastingPicker_CastingDeviceSelected(CastingDevicePicker sender, CastingDeviceSelectedEventArgs args)
        {
            CastingConnection connection = args.SelectedCastingDevice.CreateCastingConnection();
            await connection.RequestStartCastingAsync(PlayingSound.MediaPlayer.GetAsCastingSource());
        }

        private void CustomMediaTransportControls_MenuFlyoutButton_Clicked(object sender, EventArgs e)
        {
            // Update the value of the volume slider
            skipVolumeSliderValueChangedEvent = true;
            (MediaPlayerElement.TransportControls as CustomMediaTransportControls).SetVolumeSliderValue(Convert.ToInt32(PlayingSound.MediaPlayer.Volume * 100));
            skipVolumeSliderValueChangedEvent = false;
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
