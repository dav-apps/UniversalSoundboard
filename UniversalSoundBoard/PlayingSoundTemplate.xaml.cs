using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UniversalSoundBoard.Model;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media;
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
                MediaPlayerElement.SetMediaPlayer(this.PlayingSound.MediaPlayer);
                //MediaPlayerElement.TransportControls.Resources = 

                this.PlayingSound.MediaPlayer.MediaEnded += Player_MediaEnded;
                this.PlayingSound.MediaPlayer.Play();
            }
            else
            {
                PlayingSoundName.Text = "There was an error...";
            }
            setMediaPlayerElementIsCompact();
        }

        private void setDataContext()
        {
            ContentRoot.DataContext = (App.Current as App)._itemViewHolder;
        }

        private void setMediaPlayerElementIsCompact()
        {
            Debug.WriteLine(Window.Current.Bounds.Width);
            if(Window.Current.Bounds.Width < 1000 && Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily != "Windows.Mobile")
            {
                MediaPlayerElement.TransportControls.IsCompact = false;
            }else
            {
                MediaPlayerElement.TransportControls.IsCompact = true;
            }
        }

        private void StopSoundButton_Click(object sender, RoutedEventArgs e)
        {
            MediaPlayerElement.SetMediaPlayer(null);
            SoundPage.RemovePlayingSound(this.PlayingSound);
        }

        private async void Player_MediaEnded(MediaPlayer sender, object args)
        {
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                SoundPage.RemovePlayingSound(this.PlayingSound);
            });
        }

        private void CustomMediaTransportControls_Removed(object sender, EventArgs e)
        {
            Debug.WriteLine("Removed");
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            setMediaPlayerElementIsCompact();
        }

        private void CustomMediaTransportControls_Repeat_1x_Clicked(object sender, EventArgs e)
        {
            Debug.WriteLine("Repeat 1x clicked");
        }

        private void CustomMediaTransportControls_Repeat_2x_Clicked(object sender, EventArgs e)
        {
            Debug.WriteLine("Repeat 2x clicked");
        }

        private void CustomMediaTransportControls_Repeat_5x_Clicked(object sender, EventArgs e)
        {
            Debug.WriteLine("Repeat 5x clicked");
        }

        private void CustomMediaTransportControls_Repeat_10x_Clicked(object sender, EventArgs e)
        {
            Debug.WriteLine("Repeat 10x clicked");
        }

        private void CustomMediaTransportControls_Repeat_endless_Clicked(object sender, EventArgs e)
        {
            Debug.WriteLine("Repeat endless clicked");
        }
    }
}
