using Sentry;
using System;

namespace UniversalSoundboard.Common
{
    /// <summary>
    /// Adapts UWP activation/suspension to Sentry sessions. Background activation and
    /// prelaunch do not start sessions; merely hiding the window does not pause one.
    /// </summary>
    internal sealed class SessionTracking
    {
        private readonly object sync = new object();
        private bool started;
        private bool paused;

        internal static void Configure(SentryOptions options, string cacheDirectory)
        {
            // Start explicitly on user activation, not during prelaunch or AppService startup.
            options.AutoSessionTracking = false;
            options.AutoSessionTrackingInterval = TimeSpan.FromMinutes(30);
            options.CacheDirectoryPath = cacheDirectory;
        }

        internal void Activate()
        {
            lock (sync)
            {
                if (!started)
                {
                    SentrySdk.StartSession();
                    started = true;
                }
                else if (paused)
                {
                    SentrySdk.ResumeSession();
                }

                paused = false;
            }
        }

        internal void Resume()
        {
            lock (sync)
            {
                // Resuming a background-only process must not create a user session.
                if (started && paused)
                {
                    SentrySdk.ResumeSession();
                    paused = false;
                }
            }
        }

        internal void Suspend()
        {
            lock (sync)
            {
                if (started && !paused)
                {
                    // Persist the pause before Windows can terminate the suspended process.
                    SentrySdk.PauseSession();
                    paused = true;
                }
            }
        }

        internal void End()
        {
            lock (sync)
            {
                if (started)
                    SentrySdk.EndSession();

                started = false;
                paused = false;
            }
        }
    }
}
