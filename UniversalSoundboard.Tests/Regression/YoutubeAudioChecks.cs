using UniversalSoundboard.Common;
using YoutubeExplode.Videos.Streams;

internal static class YoutubeAudioChecks
{
    public static void Run(Action<bool, string> check)
    {
        var low = Audio(Container.Mp4, "mp4a.40.5", 48000);
        var high = Audio(Container.Mp4, "mp4a.40.2", 128000);
        var opus = Audio(Container.WebM, "opus", 160000);
        var manifest = new StreamManifest(new IStreamInfo[] { low, opus, high });
        check(ReferenceEquals(YoutubeAudioStream.Select(manifest), high),
            "YouTube download selects highest bitrate AAC, not higher bitrate WebM");
        check(ReferenceEquals(YoutubeAudioStream.Select(new StreamManifest(new[] { low })), low),
            "YouTube download supports a single compatible audio stream");
        foreach (var streams in new[]
        {
            Array.Empty<IStreamInfo>(),
            new IStreamInfo[] { opus },
            new IStreamInfo[] { Audio(Container.Mp4, "opus", 160000) }
        })
        {
            bool rejected = false;
            try { YoutubeAudioStream.Select(new StreamManifest(streams)); }
            catch (InvalidOperationException) { rejected = true; }
            check(rejected, "YouTube download rejects missing or incompatible M4A streams");
        }
    }

    private static AudioOnlyStreamInfo Audio(Container container, string codec, long bitrate) =>
        new AudioOnlyStreamInfo("https://example.com/audio", container, new FileSize(1024),
            new Bitrate(bitrate), codec, null, null);
}
