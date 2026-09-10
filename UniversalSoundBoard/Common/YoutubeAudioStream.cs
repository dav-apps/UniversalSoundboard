using System;
using System.Linq;
using YoutubeExplode.Videos.Streams;

namespace UniversalSoundboard.Common
{
    public static class YoutubeAudioStream
    {
        public static AudioOnlyStreamInfo Select(StreamManifest manifest)
        {
            // The download is stored as M4A. WebM/Opus cannot be relabelled as M4A.
            return manifest.GetAudioOnlyStreams()
                .Where(stream => stream.Container == Container.Mp4
                    && stream.AudioCodec.StartsWith("mp4a", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(stream => stream.Bitrate)
                .FirstOrDefault()
                ?? throw new InvalidOperationException("No compatible M4A audio stream is available.");
        }
    }
}
