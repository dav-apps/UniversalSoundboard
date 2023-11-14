using System;
using UniversalSoundboard.Models;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Components
{
    public sealed partial class StoreSoundTileTemplate : UserControl
    {
        public SoundResponse SoundItem { get; set; }
        private bool isPlaying = false;

        public event EventHandler<EventArgs> Play;
        public event EventHandler<EventArgs> Pause;

        public StoreSoundTileTemplate()
        {
            InitializeComponent();
        }

        private void UserControl_DataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
        {
            if (DataContext == null) return;

            SoundItem = DataContext as SoundResponse;
            Bindings.Update();
        }

        private void PlayPauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (isPlaying)
            {
                isPlaying = false;
                Pause?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                isPlaying = true;
                Play?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
