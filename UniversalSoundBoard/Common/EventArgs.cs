using System;
using Windows.UI.Xaml;

namespace UniversalSoundboard.Common
{
    #region ItemViewHolder
    public class CategoryEventArgs : EventArgs
    {
        public Guid Uuid { get; set; }

        public CategoryEventArgs(Guid uuid)
        {
            Uuid = uuid;
        }
    }

    public class PlayingSoundItemEventArgs : EventArgs
    {
        public Guid Uuid { get; set; }
        public double HeightDifference { get; set; }

        public PlayingSoundItemEventArgs(Guid uuid)
        {
            Uuid = uuid;
            HeightDifference = 0;
        }

        public PlayingSoundItemEventArgs(Guid uuid, double heightDifference)
        {
            Uuid = uuid;
            HeightDifference = heightDifference;
        }
    }
    #endregion

    #region PlayingSoundItem
    public class PlaybackStateChangedEventArgs : EventArgs
    {
        public bool IsPlaying { get; }

        public PlaybackStateChangedEventArgs(bool isPlaying)
        {
            IsPlaying = isPlaying;
        }
    }

    public class CurrentSoundChangedEventArgs : EventArgs
    {
        public int CurrentSound { get; }

        public CurrentSoundChangedEventArgs(int currentSound)
        {
            CurrentSound = currentSound;
        }
    }

    public class ButtonVisibilityChangedEventArgs : EventArgs
    {
        public Visibility PreviousButtonVisibility { get; }
        public Visibility NextButtonVisibility { get; }
        public Visibility ExpandButtonVisibility { get; }

        public ButtonVisibilityChangedEventArgs(
            Visibility previousButtonVisibility,
            Visibility nextButtonVisibility,
            Visibility expandButtonVisibility
        )
        {
            PreviousButtonVisibility = previousButtonVisibility;
            NextButtonVisibility = nextButtonVisibility;
            ExpandButtonVisibility = expandButtonVisibility;
        }
    }

    public class ExpandButtonContentChangedEventArgs : EventArgs
    {
        public bool Expanded { get; }

        public ExpandButtonContentChangedEventArgs(bool expanded)
        {
            Expanded = expanded;
        }
    }

    public class FavouriteChangedEventArgs : EventArgs
    {
        public bool Favourite { get; }

        public FavouriteChangedEventArgs(bool favourite)
        {
            Favourite = favourite;
        }
    }

    public class VolumeChangedEventArgs : EventArgs
    {
        public int Volume { get; }

        public VolumeChangedEventArgs(int volume)
        {
            Volume = volume;
        }
    }

    public class MutedChangedEventArgs : EventArgs
    {
        public bool Muted { get; }

        public MutedChangedEventArgs(bool muted)
        {
            Muted = muted;
        }
    }
    #endregion
}
