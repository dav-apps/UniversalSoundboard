using System;
using UniversalSoundboard.Common;
using UniversalSoundboard.Components;
using Windows.UI.Xaml;

namespace UniversalSoundboard.Models
{
    public class PlayingSoundItemContainer
    {
        public int Index { get; }
        public PlayingSound PlayingSound { get; }
        public PlayingSoundItemTemplate PlayingSoundItemTemplate { get; set; }
        public double ContentHeight { get; set; }
        public bool IsVisible { get; set; }
        public bool IsLoaded { get; private set; }
        public bool IsInBottomPlayingSoundsBar { get; set; }
        public bool ShowAnimations { get; set; }

        public event EventHandler<EventArgs> Hide;
        public event EventHandler<EventArgs> Loaded;
        public event EventHandler<SizeChangedEventArgs> SizeChanged;
        public event EventHandler<PlayingSoundSoundsListEventArgs> ExpandSoundsList;
        public event EventHandler<PlayingSoundSoundsListEventArgs> CollapseSoundsList;

        public PlayingSoundItemContainer(
            int index,
            PlayingSound playingSound,
            bool isInBottomPlayingSoundsBar,
            bool showAnimations = true
        )
        {
            Index = index;
            PlayingSound = playingSound;
            ContentHeight = 0;
            IsVisible = true;
            IsLoaded = false;
            IsInBottomPlayingSoundsBar = isInBottomPlayingSoundsBar;
            ShowAnimations = showAnimations;
        }
        
        public void TriggerHideEvent(EventArgs args)
        {
            Hide?.Invoke(this, args);
        }

        public void TriggerLoadedEvent(EventArgs args)
        {
            IsLoaded = true;
            Loaded?.Invoke(this, args);
        }

        public void TriggerSizeChangedEvent(SizeChangedEventArgs args)
        {
            SizeChanged?.Invoke(this, args);
        }

        public void TriggerExpandSoundsListEvent(PlayingSoundSoundsListEventArgs args)
        {
            ExpandSoundsList?.Invoke(this, args);
        }

        public void TriggerCollapseSoundsListEvent(PlayingSoundSoundsListEventArgs args)
        {
            CollapseSoundsList?.Invoke(this, args);
        }
    }
}
