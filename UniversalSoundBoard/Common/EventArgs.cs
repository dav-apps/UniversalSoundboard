using System;
using System.IO;
using UniversalSoundboard.Models;
using Windows.Media;
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

    public class SoundEventArgs
    {
        public Guid Uuid { get; set; }

        public SoundEventArgs(Guid uuid)
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

    public class TableObjectFileDownloadProgressChangedEventArgs : EventArgs
    {
        public Guid Uuid { get; set; }
        public int Value { get; set; }

        public TableObjectFileDownloadProgressChangedEventArgs(Guid uuid, int value)
        {
            Uuid = uuid;
            Value = value;
        }
    }

    public class TableObjectFileDownloadCompletedEventArgs : EventArgs
    {
        public Guid Uuid { get; set; }
        public FileInfo File { get; set; }

        public TableObjectFileDownloadCompletedEventArgs(Guid uuid, FileInfo file)
        {
            Uuid = uuid;
            File = file;
        }
    }

    public class ShowInAppNotificationEventArgs : EventArgs
    {
        public InAppNotificationType Type { get; set; }
        public string Message { get; set; }
        public int Duration { get; set; }
        public bool ShowProgressRing { get; set; }
        public bool Dismissable { get; set; }
        public string PrimaryButtonText { get; set; }
        public string SecondaryButtonText { get; set; }
        public event EventHandler<RoutedEventArgs> PrimaryButtonClick;
        public event EventHandler<RoutedEventArgs> SecondaryButtonClick;

        public ShowInAppNotificationEventArgs(
            InAppNotificationType type,
            string message,
            int duration = 0,
            bool showProgressRing = false,
            bool dismissable = false,
            string primaryButtonText = null,
            string secondaryButtonText = null
        ) {
            Type = type;
            Message = message;
            Duration = duration;
            ShowProgressRing = showProgressRing;
            Dismissable = dismissable;
            PrimaryButtonText = primaryButtonText;
            SecondaryButtonText = secondaryButtonText;
        }

        public void TriggerPrimaryButtonClickEvent(object sender, RoutedEventArgs args)
        {
            PrimaryButtonClick?.Invoke(sender, args);
        }

        public void TriggerSecondaryButtonClickEvent(object sender, RoutedEventArgs args)
        {
            SecondaryButtonClick?.Invoke(sender, args);
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

    public class PositionChangedEventArgs : EventArgs
    {
        public TimeSpan Position { get; set; }

        public PositionChangedEventArgs(TimeSpan position)
        {
            Position = position;
        }
    }

    public class DurationChangedEventArgs : EventArgs
    {
        public TimeSpan Duration { get; set; }

        public DurationChangedEventArgs(TimeSpan duration)
        {
            Duration = duration;
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

    public class LocalFileButtonVisibilityEventArgs : EventArgs
    {
        public Visibility LocalFileButtonVisibility { get; set; }

        public LocalFileButtonVisibilityEventArgs(Visibility localFileButtonVisibility)
        {
            LocalFileButtonVisibility = localFileButtonVisibility;
        }
    }

    public class OutputDeviceButtonVisibilityEventArgs : EventArgs
    {
        public Visibility OutputDeviceButtonVisibility { get; }

        public OutputDeviceButtonVisibilityEventArgs(Visibility outputDeviceButtonVisibility)
        {
            OutputDeviceButtonVisibility = outputDeviceButtonVisibility;
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

    public class RepetitionsChangedEventArgs : EventArgs
    {
        public int Repetitions { get; }

        public RepetitionsChangedEventArgs(int repetitions)
        {
            Repetitions = repetitions;
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

    public class PlaybackSpeedChangedEventArgs : EventArgs
    {
        public int PlaybackSpeed { get; }

        public PlaybackSpeedChangedEventArgs(int playbackSpeed)
        {
            PlaybackSpeed = playbackSpeed;
        }
    }

    public class DownloadStatusChangedEventArgs : EventArgs
    {
        public bool IsDownloading { get; set; }
        public int DownloadProgress { get; set; }

        public DownloadStatusChangedEventArgs(bool isDownloading, int downloadProgress)
        {
            IsDownloading = isDownloading;
            DownloadProgress = downloadProgress;
        }
    }
    #endregion

    #region HotkeyItem
    public class HotkeyEventArgs : EventArgs
    {
        public Hotkey Hotkey { get; set; }

        public HotkeyEventArgs(Hotkey hotkey)
        {
            Hotkey = hotkey;
        }
    }
    #endregion

    #region AudioRecorder
    public class AudioRecorderQuantumStartedEventArgs : EventArgs
    {
        public AudioFrame AudioFrame { get; set; }

        public AudioRecorderQuantumStartedEventArgs(AudioFrame audioFrame)
        {
            AudioFrame = audioFrame;
        }
    }
    #endregion
}
