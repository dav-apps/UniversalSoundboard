using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UniversalSoundBoard.Model;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Playback;
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
        public PlayingSound PlayingSound { get { return this.DataContext as PlayingSound; } }

        CoreDispatcher dispatcher;

        public PlayingSoundTemplate()
        {
            this.InitializeComponent();
            Loaded += PlayingSoundTemplate_Loaded;
            this.DataContextChanged += (s, e) => Bindings.Update();

            dispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
        }

        private void PlayingSoundTemplate_Loaded(object sender, RoutedEventArgs e)
        {
            setDataContext();

            if(this.PlayingSound != null)
            {
                MediaPlayerElement.SetMediaPlayer(this.PlayingSound.Player);

                MediaPlayerElement.TransportControls = new MediaTransportControls()
                {
                    IsCompact = true,
                    IsFullWindowButtonVisible = false,
                    IsZoomButtonVisible = false
                };

                this.PlayingSound.Player.MediaEnded += Player_MediaEnded;
                this.PlayingSound.Player.Play();
            }else
            {
                PlayingSoundName.Text = "There was an error...";
            }
        }

        private void setDataContext()
        {
            ContentRoot.DataContext = (App.Current as App)._itemViewHolder;
        }

        private void StopSoundButton_Click(object sender, RoutedEventArgs e)
        {
            this.PlayingSound.Player.Pause();
            MediaPlayerElement.SetMediaPlayer(null);
            //(App.Current as App)._itemViewHolder.playingSounds.Remove(this.PlayingSound);
            SoundPage.RemovePlayingSound(this.PlayingSound);
        }

        private async void Player_MediaEnded(MediaPlayer sender, object args)
        {
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                //(App.Current as App)._itemViewHolder.playingSounds.Remove(PlayingSound.GetPlayingSoundByMediaPlayer(sender));
                SoundPage.RemovePlayingSound(this.PlayingSound);
            });
        }
    }
}
