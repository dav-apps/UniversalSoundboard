using System;
using Windows.Media.Audio;

namespace UniversalSoundboard.Common
{
    class AudioIOException : Exception { }

    class AudioPlayerNotInitializedException : AudioIOException { }

    class AudioRecorderNotInitializedException : AudioIOException { }

    class AudioRecorderDisposedException : AudioIOException { }

    enum AudioPlayerInitError
    {
        AudioFileNotSpecified
    }

    class AudioPlayerInitException : AudioIOException
    {
        public AudioPlayerInitError Error;

        public AudioPlayerInitException(AudioPlayerInitError error)
        {
            Error = error;
        }
    }

    enum AudioRecorderInitError
    {
        AudioFileNotSpecified,
        NotInitialized,
        NotAllowedWhileRecording,
        NoOutputSpecified
    }

    class AudioRecorderInitException : AudioIOException
    {
        public AudioRecorderInitError Error;

        public AudioRecorderInitException(AudioRecorderInitError error)
        {
            Error = error;
        }
    }

    enum AudioGraphInitError
    {
        DeviceNotAvailable,
        FormatNotSupported,
        UnknownFailure
    }

    class AudioGraphInitException : AudioIOException
    {
        public AudioGraphInitError Error;

        public AudioGraphInitException(AudioGraphCreationStatus status)
        {
            switch (status)
            {
                case AudioGraphCreationStatus.DeviceNotAvailable:
                    Error = AudioGraphInitError.DeviceNotAvailable;
                    break;
                case AudioGraphCreationStatus.FormatNotSupported:
                    Error = AudioGraphInitError.FormatNotSupported;
                    break;
                default:
                    Error = AudioGraphInitError.UnknownFailure;
                    break;
            }
        }
    }

    enum FileInputNodeInitError
    {
        FileNotFound,
        InvalidFileType,
        FormatNotSupported,
        UnknownFailure
    }

    class FileInputNodeInitException : AudioIOException
    {
        public FileInputNodeInitError Error;

        public FileInputNodeInitException(AudioFileNodeCreationStatus status)
        {
            switch (status)
            {
                case AudioFileNodeCreationStatus.FileNotFound:
                    Error = FileInputNodeInitError.FileNotFound;
                    break;
                case AudioFileNodeCreationStatus.InvalidFileType:
                    Error = FileInputNodeInitError.InvalidFileType;
                    break;
                case AudioFileNodeCreationStatus.FormatNotSupported:
                    Error = FileInputNodeInitError.FormatNotSupported;
                    break;
                default:
                    Error = FileInputNodeInitError.UnknownFailure;
                    break;
            }
        }
    }

    enum DeviceInputNodeInitError
    {
        DeviceNotAvailable,
        FormatNotSupported,
        UnknownFailure,
        AccessDenied
    }

    class DeviceInputNodeInitException : AudioIOException
    {
        public DeviceInputNodeInitError Error;

        public DeviceInputNodeInitException(AudioDeviceNodeCreationStatus status)
        {
            switch (status)
            {
                case AudioDeviceNodeCreationStatus.DeviceNotAvailable:
                    Error = DeviceInputNodeInitError.DeviceNotAvailable;
                    break;
                case AudioDeviceNodeCreationStatus.FormatNotSupported:
                    Error = DeviceInputNodeInitError.FormatNotSupported;
                    break;
                case AudioDeviceNodeCreationStatus.AccessDenied:
                    Error = DeviceInputNodeInitError.AccessDenied;
                    break;
                default:
                    Error = DeviceInputNodeInitError.UnknownFailure;
                    break;
            }
        }
    }

    enum FileOutputNodeInitError
    {
        FileNotFound,
        InvalidFileType,
        FormatNotSupported,
        UnknownFailure
    }

    class FileOutputNodeInitException : AudioIOException
    {
        public FileOutputNodeInitError Error;

        public FileOutputNodeInitException(AudioFileNodeCreationStatus status)
        {
            switch (status)
            {
                case AudioFileNodeCreationStatus.FileNotFound:
                    Error = FileOutputNodeInitError.FileNotFound;
                    break;
                case AudioFileNodeCreationStatus.InvalidFileType:
                    Error = FileOutputNodeInitError.InvalidFileType;
                    break;
                case AudioFileNodeCreationStatus.FormatNotSupported:
                    Error = FileOutputNodeInitError.FormatNotSupported;
                    break;
                default:
                    Error = FileOutputNodeInitError.UnknownFailure;
                    break;
            }
        }
    }

    enum DeviceOutputNodeInitError
    {
        DeviceNotAvailable,
        FormatNotSupported,
        UnknownFailure,
        AccessDenied
    }

    class DeviceOutputNodeInitException : AudioIOException
    {
        public DeviceOutputNodeInitError Error;

        public DeviceOutputNodeInitException(AudioDeviceNodeCreationStatus status)
        {
            switch (status)
            {
                case AudioDeviceNodeCreationStatus.DeviceNotAvailable:
                    Error = DeviceOutputNodeInitError.DeviceNotAvailable;
                    break;
                case AudioDeviceNodeCreationStatus.FormatNotSupported:
                    Error = DeviceOutputNodeInitError.FormatNotSupported;
                    break;
                case AudioDeviceNodeCreationStatus.AccessDenied:
                    Error = DeviceOutputNodeInitError.AccessDenied;
                    break;
                default:
                    Error = DeviceOutputNodeInitError.UnknownFailure;
                    break;
            }
        }
    }

    class SoundDownloadException : Exception { }
}
