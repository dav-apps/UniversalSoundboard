using System;
using Windows.Media.Audio;

namespace UniversalSoundboard.Common
{
    class AudioPlayerNotInitializedException : Exception { }

    class AudioRecorderNotInitializedException : Exception { }

    enum AudioPlayerInitError
    {
        AudioFileNotSpecified = 0
    }

    class AudioPlayerInitException : Exception
    {
        public AudioPlayerInitError Error;

        public AudioPlayerInitException(AudioPlayerInitError error)
        {
            Error = error;
        }
    }

    enum AudioRecorderInitError
    {
        AudioFileNotSpecified = 0,
        NotInitialized = 1,
        NotAllowedWhileRecording = 2,
        NoOutputSpecified = 3
    }

    class AudioRecorderInitException : Exception
    {
        public AudioRecorderInitError Error;

        public AudioRecorderInitException(AudioRecorderInitError error)
        {
            Error = error;
        }
    }

    enum AudioGraphInitError
    {
        DeviceNotAvailable = 1,
        FormatNotSupported = 2,
        UnknownFailure = 3
    }

    class AudioGraphInitException : Exception
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
        FileNotFound = 1,
        InvalidFileType = 2,
        FormatNotSupported = 3,
        UnknownFailure = 4
    }

    class FileInputNodeInitException : Exception
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
        DeviceNotAvailable = 1,
        FormatNotSupported = 2,
        UnknownFailure = 3,
        AccessDenied = 4
    }

    class DeviceInputNodeInitException : Exception
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
        FileNotFound = 1,
        InvalidFileType = 2,
        FormatNotSupported = 3,
        UnknownFailure = 4
    }

    class FileOutputNodeInitException : Exception
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
        DeviceNotAvailable = 1,
        FormatNotSupported = 2,
        UnknownFailure = 3,
        AccessDenied = 4
    }

    class DeviceOutputNodeInitException : Exception
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
}
