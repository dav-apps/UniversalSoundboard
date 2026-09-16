using System.Collections.Concurrent;
using System.Text.Json;
using Sentry;
using Sentry.Extensibility;
using Sentry.Protocol;
using Sentry.Protocol.Envelopes;
using UniversalSoundboard.Common;

internal static class SessionTrackingChecks
{
    public static async Task Run(Action<bool, string> check)
    {
        string cache = Path.Combine(Path.GetTempPath(), "UniversalSoundboard-session-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(cache);
        try
        {
            var transport = new RecordingTransport();
            SentryOptions configured = null;
            using (Initialize(cache, transport, options => configured = options))
            {
                var sessions = new SessionTracking();
                sessions.Suspend();
                sessions.Resume();
                await Flush();
                check(transport.Sessions.IsEmpty, "prelaunch/background resume does not count as user activity");
                check(!configured.AutoSessionTracking && configured.AutoSessionTrackingInterval == TimeSpan.FromMinutes(30),
                    "explicit activation and 30 minute suspension threshold configured");

                sessions.Activate();
                sessions.Activate(); // Protocol callback or repeated launch.
                await Flush();
                var initial = transport.Sessions.Single();
                check(initial.GetProperty("init").GetBoolean(), "first activation creates a session");
                check(initial.GetProperty("attrs").GetProperty("release").GetString() == "session-tests@1.0"
                    && initial.GetProperty("attrs").GetProperty("environment").GetString() == "test",
                    "sessions carry release and environment");
                check(!string.IsNullOrWhiteSpace(initial.GetProperty("did").GetString()), "session has a stable installation identifier");

                sessions.Suspend();
                sessions.Suspend();
                check(Directory.GetFiles(cache, ".session", SearchOption.AllDirectories).Length == 1,
                    "suspension persists session before OS termination");
                sessions.Resume();
                sessions.Activate();
                await Flush();
                check(transport.Sessions.Count == 1, "short suspension and repeated activation preserve the session");

                SentrySdk.CaptureMessage("ordinary app usage", SentryLevel.Info);
                await Flush();
                check(transport.Sessions.Count == 1, "informational events do not mark session as errored");
                SentrySdk.CaptureException(new InvalidOperationException("handled test failure"));
                await Flush();
                check(transport.Sessions.Last().GetProperty("errors").GetInt32() > 0
                    && Status(transport.Sessions.Last()) == "ok",
                    "handled error updates the active session without reporting a crash");

                configured.AutoSessionTrackingInterval = TimeSpan.Zero; // Exercise expiry without wall-clock waits.
                sessions.Suspend();
                sessions.Resume();
                await Flush();
                check(transport.Sessions.Count(s => s.TryGetProperty("init", out var init) && init.GetBoolean()) == 2,
                    "expired suspension starts a new session");
                check(transport.Sessions.Any(s => Status(s) == "exited"),
                    "expired session ends normally");
                check(transport.Sessions.Last().GetProperty("did").GetString() == initial.GetProperty("did").GetString(),
                    "new sessions retain installation identity");

                var crash = new InvalidOperationException("controlled terminal test failure");
                crash.Data[Mechanism.HandledKey] = false;
                crash.Data[Mechanism.TerminalKey] = true;
                crash.Data[Mechanism.MechanismKey] = "Application.UnhandledException";
                SentrySdk.CaptureException(crash);
                await Flush();
                check(Status(transport.Sessions.Last()) == "crashed",
                    "UWP terminal exception ends session as crashed");
                sessions.End();
                await Flush();
                check(Status(transport.Sessions.Last()) == "crashed",
                    "window close cannot overwrite a crash with normal exit");
            }

            // Preserve the real SDK's paused-session file to simulate OS termination while suspended.
            string persistedPath;
            byte[] persistedSession;
            string installation;
            var recoveryTransport = new RecordingTransport();
            using (Initialize(cache, recoveryTransport))
            {
                var sessions = new SessionTracking();
                sessions.Activate();
                sessions.Suspend();
                await Flush();
                installation = recoveryTransport.Sessions.Single().GetProperty("did").GetString();
                persistedPath = Directory.GetFiles(cache, ".session", SearchOption.AllDirectories).Single();
                persistedSession = File.ReadAllBytes(persistedPath);
            }
            File.WriteAllBytes(persistedPath, persistedSession);
            recoveryTransport = new RecordingTransport();
            using (Initialize(cache, recoveryTransport))
            {
                var sessions = new SessionTracking();
                sessions.Activate();
                await Flush();
                check(recoveryTransport.Sessions.Any(s => Status(s) == "exited"),
                    "restart recovers OS-terminated suspended session as normal exit");
                check(recoveryTransport.Sessions.Last().GetProperty("did").GetString() == installation,
                    "installation identity survives SDK restart");
                sessions.End();
                sessions.End();
                await Flush();
                check(recoveryTransport.Sessions.Count(s => Status(s) == "exited") == 2,
                    "normal close ends current session exactly once");
            }

            // A failed transport must leave envelopes on disk for the next SDK startup.
            using (Initialize(cache, new RecordingTransport { Offline = true }))
            {
                var sessions = new SessionTracking();
                sessions.Activate();
                sessions.End();
                await Flush();
            }
            var online = new RecordingTransport();
            using (Initialize(cache, online))
            {
                await Flush();
                check(online.Sessions.Any(s => s.TryGetProperty("init", out var init) && init.GetBoolean())
                    && online.Sessions.Any(s => Status(s) == "exited"),
                    "offline session envelopes are delivered after restart");
            }
        }
        finally
        {
            Directory.Delete(cache, true);
        }
    }

    private static IDisposable Initialize(string cache, RecordingTransport transport, Action<SentryOptions> configure = null) =>
        SentrySdk.Init(options =>
        {
            options.Dsn = "https://public@example.invalid/1";
            options.Release = "session-tests@1.0";
            options.Environment = "test";
            options.IsGlobalModeEnabled = true;
            options.Transport = transport; // No network requests or production telemetry.
            options.InitCacheFlushTimeout = TimeSpan.FromSeconds(5);
            SessionTracking.Configure(options, cache);
            configure?.Invoke(options);
        });

    private static Task Flush() => SentrySdk.FlushAsync(TimeSpan.FromSeconds(5));

    private static string Status(JsonElement session) =>
        session.TryGetProperty("status", out var status) ? status.GetString() : "ok";

    private sealed class RecordingTransport : ITransport
    {
        public ConcurrentQueue<JsonElement> Sessions { get; } = new();
        public bool Offline { get; init; }

        public async Task SendEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken = default)
        {
            if (Offline) throw new HttpRequestException("Simulated offline transport");
            foreach (var item in envelope.Items.Where(item => item.Header["type"].ToString() == "session"))
            {
                using var stream = new MemoryStream();
                await item.Payload.SerializeAsync(stream, null, cancellationToken);
                using var document = JsonDocument.Parse(stream.ToArray());
                Sessions.Enqueue(document.RootElement.Clone());
            }
        }
    }
}
