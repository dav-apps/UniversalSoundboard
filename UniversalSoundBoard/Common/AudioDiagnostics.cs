using Sentry;
using System;
using System.Collections;
using System.Diagnostics;

namespace UniversalSoundboard.Common
{
    internal static class AudioDiagnostics
    {
        public static void Annotate(Exception exception, string stage, int outputCount, string fileType)
        {
            if (!exception.Data.Contains("audio.stage")) exception.Data["audio.stage"] = stage;
            if (!exception.Data.Contains("audio.status")) exception.Data["audio.status"] = exception.GetType().Name;
            exception.Data["audio.output_count"] = outputCount.ToString();
            exception.Data["audio.file_type"] = fileType ?? "none";
            exception.Data["audio.hresult"] = $"0x{exception.HResult:X8}";
            // Preserve the original trace and a separately labelled capture-site fallback for .NET Native.
            try { exception.Data["audio.capture_stack"] = new StackTrace(true).ToString(); }
            catch (Exception) { exception.Data["audio.capture_stack"] = "Unavailable on this runtime"; }
        }

        public static SentryEvent EnrichEvent(SentryEvent sentryEvent)
        {
            var exception = sentryEvent.Exception;
            if (exception == null) return sentryEvent;
            bool audioException = false;
            for (var current = exception; current != null; current = current.InnerException)
            {
                foreach (DictionaryEntry entry in current.Data)
                {
                    var key = entry.Key as string;
                    if (key == null || !key.StartsWith("audio.", StringComparison.Ordinal)) continue;
                    audioException = true;
                    if (key == "audio.capture_stack") sentryEvent.SetExtra(key, entry.Value);
                    else sentryEvent.SetTag(key, Convert.ToString(entry.Value));
                }
            }
            if (audioException) sentryEvent.SetExtra("audio.exception_details", exception.ToString());
            return sentryEvent;
        }
    }
}
