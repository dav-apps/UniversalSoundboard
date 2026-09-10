using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace UniversalSoundboard.Common
{
    public static class YoutubeUrl
    {
        private static bool TryGetUri(string input, out Uri uri)
        {
            uri = null;
            if (string.IsNullOrWhiteSpace(input)) return false;
            input = input.Trim();
            if (!input.Contains("://")) input = "https://" + input;
            return Uri.TryCreate(input, UriKind.Absolute, out uri)
                && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
        }

        public static bool IsYoutubeHost(string input, bool shortLink)
        {
            if (!TryGetUri(input, out var uri)) return false;
            var host = uri.Host.ToLowerInvariant();
            return shortLink ? host == "youtu.be" || host == "www.youtu.be"
                : host == "youtube.com" || host == "www.youtube.com"
                    || host == "music.youtube.com" || host == "m.youtube.com";
        }

        public static bool TryParse(string input, out string videoId, out string playlistId)
        {
            videoId = null;
            playlistId = null;
            if (!TryGetUri(input, out var uri)) return false;
            bool shortLink = IsYoutubeHost(input, true);
            if (!shortLink && !IsYoutubeHost(input, false)) return false;

            var query = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var part in uri.Query.TrimStart('?').Split('&'))
            {
                var pair = part.Split(new[] { '=' }, 2);
                if (pair.Length == 2)
                    query[Uri.UnescapeDataString(pair[0])] = Uri.UnescapeDataString(pair[1]);
            }

            var segments = uri.AbsolutePath.Trim('/').Split('/');
            string candidate = null;
            if (shortLink && segments.Length == 1) candidate = segments[0];
            else if (!shortLink && segments.Length == 1 && segments[0] == "watch")
                query.TryGetValue("v", out candidate);
            else if (!shortLink && segments.Length == 2
                && (segments[0] == "shorts" || segments[0] == "live" || segments[0] == "embed"))
                candidate = segments[1];

            if (candidate == null || !Regex.IsMatch(candidate, "^[a-zA-Z0-9_-]{11}$")) return false;
            videoId = candidate;
            if (query.TryGetValue("list", out var list) && Regex.IsMatch(list, "^[a-zA-Z0-9_-]+$"))
                playlistId = list;
            return true;
        }
    }
}
