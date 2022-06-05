using System;
using Windows.Media.Audio;

namespace UniversalSoundboard.Common
{
    enum AudioPlayerInitError
    {
        FileNotFound = 1,
        InvalidFileType = 2,
        FileFormatNotSupported = 3,
        OutputDeviceNotAvailable = 4,
        OutputDeviceAccessDenied = 5,
        UnknownFailure = 6
    }

    class AudioPlayerInitException : Exception
    {
        public AudioPlayerInitError Error { get; set; }

        public AudioPlayerInitException(AudioPlayerInitError error)
        {
            Error = error;
        }

        public AudioPlayerInitException(AudioGraphCreationStatus status)
        {
            switch (status)
            {
                case AudioGraphCreationStatus.DeviceNotAvailable:
                    Error = AudioPlayerInitError.OutputDeviceNotAvailable;
                    break;
                case AudioGraphCreationStatus.FormatNotSupported:
                    Error = AudioPlayerInitError.FileFormatNotSupported;
                    break;
                default:
                    Error = AudioPlayerInitError.UnknownFailure;
                    break;
            }
        }

        public AudioPlayerInitException(AudioFileNodeCreationStatus status)
        {
            switch (status)
            {
                case AudioFileNodeCreationStatus.FileNotFound:
                    Error = AudioPlayerInitError.FileNotFound;
                    break;
                case AudioFileNodeCreationStatus.InvalidFileType:
                    Error = AudioPlayerInitError.InvalidFileType;
                    break;
                case AudioFileNodeCreationStatus.FormatNotSupported:
                    Error = AudioPlayerInitError.FileFormatNotSupported;
                    break;
                default:
                    Error = AudioPlayerInitError.UnknownFailure;
                    break;
            }
        }

        public AudioPlayerInitException(AudioDeviceNodeCreationStatus status)
        {
            switch(status)
            {
                case AudioDeviceNodeCreationStatus.DeviceNotAvailable:
                    Error = AudioPlayerInitError.OutputDeviceNotAvailable;
                    break;
                case AudioDeviceNodeCreationStatus.FormatNotSupported:
                    Error = AudioPlayerInitError.FileFormatNotSupported;
                    break;
                case AudioDeviceNodeCreationStatus.AccessDenied:
                    Error = AudioPlayerInitError.OutputDeviceAccessDenied;
                    break;
                default:
                    Error = AudioPlayerInitError.UnknownFailure;
                    break;
            }
        }
    }

    class AudioPlayerNotInitializedException : Exception { }
}
