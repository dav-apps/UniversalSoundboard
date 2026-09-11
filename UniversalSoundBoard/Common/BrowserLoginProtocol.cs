using System;
using System.Web;

namespace UniversalSoundboard.Common
{
    internal static class BrowserLoginProtocol
    {
        public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(10);

        public static Uri CreateRequest(string website, int appId, string apiKey, bool signup, string state)
        {
            string callback = "universalsoundboard://login?state=" + Uri.EscapeDataString(state);
            return new Uri($"{website}/{(signup ? "signup" : "login")}?appId={appId}"
                + "&apiKey=" + Uri.EscapeDataString(apiKey)
                + "&redirectUrl=" + Uri.EscapeDataString(callback));
        }

        public static bool IsCallback(Uri uri) => uri != null && uri.IsAbsoluteUri
            && uri.Scheme == "universalsoundboard" && uri.Host == "login"
            && (uri.AbsolutePath == "" || uri.AbsolutePath == "/")
            && uri.UserInfo == "" && uri.Port == -1 && uri.Fragment == "";

        public static bool TryGetToken(Uri uri, string state, DateTimeOffset started,
            DateTimeOffset now, out string token)
        {
            token = null;
            if (!IsCallback(uri) || string.IsNullOrEmpty(state)
                || now < started || now - started >= Lifetime) return false;

            var query = HttpUtility.ParseQueryString(uri.Query);
            var states = query.GetValues("state");
            var tokens = query.GetValues("accessToken");
            if (states?.Length != 1 || states[0] != state || tokens?.Length != 1
                || string.IsNullOrWhiteSpace(tokens[0])) return false;

            token = tokens[0];
            return true;
        }
    }
}
