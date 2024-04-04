namespace UniversalSoundboard.Common
{
    public enum AppWindowType
    {
        Main,
        SoundRecorder
    }

    public enum SoundOrder
    {
        Custom = 0,
        NameAscending = 1,
        NameDescending = 2,
        CreationDateAscending = 3,
        CreationDateDescending = 4
    }

    public enum DataModel
    {
        Old,
        New,
        Dav
    }

    public enum AppTheme
    {
        System,
        Light,
        Dark
    }

    public enum AppState
    {
        Loading,
        InitialSync,
        Empty,
        Normal
    }

    public enum Modifiers
    {
        None = 0,
        Alt = 1,
        Control = 2,
        AltControl = 3,
        Shift = 4,
        AltShift = 5,
        ControlShift = 6,
        AltControlShift = 7,
        Windows = 8,
        AltWindows = 9,
        ControlWindows = 10,
        AltControlWindows = 11,
        ShiftWindows = 12,
        AltShiftWindows = 13,
        ControlShiftWindows = 14
    }

    public enum DownloadSoundsResultType
    {
        None,
        Youtube,
        AudioFile
    }

    public enum InAppNotificationType
    {
        AddSounds,
        DownloadSound,
        DownloadSounds,
        ContinuePlaylistDownload,
        Export,
        Import,
        ImageExport,
        SoundExport,
        SoundsExport,
        Sync,
        WriteReview
    }

    enum PlayingSoundItemLayoutType
    {
        SingleSoundSmall,
        SingleSoundLarge,
        Compact,
        Mini,
        Small,
        Large
    }

    public enum BottomPlayingSoundsBarVerticalPosition
    {
        Top,
        Bottom
    }

    public enum PlaybackState
    {
        Playing,
        FadeOut,
        Paused
    }
}
