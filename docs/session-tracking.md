# Sentry release-health sessions

Session tracking uses the existing Sentry 6.11.0 SDK and release/environment configuration.

## Session boundaries

- Normal launch, protocol activation (including browser-login callbacks), opening files and share-target activation start a session if none is active. Repeated activations keep the current session.
- Windows prelaunch and AppService/background activation alone do not start user sessions.
- Minimizing, switching applications and background audio playback do not split a session.
- `Suspending` pauses/persists the session before flushing telemetry, with the suspension deferral always completed. `Resuming` continues an existing session. After at least 30 minutes suspended, the SDK ends the previous session and starts a new one.
- If delivered, `Window.Closed` ends the session normally and flushes with a two-second timeout. UWP can instead suspend/terminate the process when the user closes the window (observed in the desktop check). The SDK then recovers the persisted paused session as exited on the next user activation.
- The UWP unhandled-exception handler marks terminating exceptions as terminal. The SDK attaches a crashed session update to that event. Handled errors can mark sessions as errored without counting as crashes; informational usage events do not.

## Persistence and interpretation

The SDK cache lives under `ApplicationData.Current.LocalCacheFolder/Sentry/{environment}`. It persists pending envelopes, session state and the SDK installation identifier. Debug and production caches are separate. Offline envelopes are retried by the SDK, including on the next startup; this is bounded storage, not a guarantee against every loss of power or disk failure.

Sentry session user counts use its persisted installation identifier, which differs from the app's existing `user.id` on error/business events. Compare session-based users within Release Health; do not join those IDs directly with previous event-based installation counts. Neither metric measures all Store installations. Cache deletion or reinstallation can reset the SDK identifier.

An abrupt termination without a captured terminal exception is not proof of a crash: the SDK can recover it as abnormal. No native crash detector is added. Initial-session and crash updates arrive during use; recovery of an OS-terminated paused session requires the next activation. Previous releases do not acquire session data retroactively.

## Verification

`dotnet run --project UniversalSoundboard.Tests/Regression/Regression.csproj`

The suite exercises the real SDK with a local recording transport and a dummy DSN, without sending test events to Sentry. It covers activation deduplication, background-only resume, short/expired suspension, release/environment and persistent installation identity, handled versus terminal errors, normal close, persisted-session recovery and offline delivery after restart.

Validation on 2026-09-16: all 166 regression checks and x64 Debug/Release package builds passed. The development app created its session cache with the expected release and debug environment, and the Sentry sessions API confirmed a healthy session for `UniversalSoundboard@2.5.10.3`. Desktop checks verified that minimizing/restoring preserves the session, closing persists its paused state, and reopening creates a new session with the same installation identity. Terminal-error and offline scenarios were exercised with the isolated SDK transport, not by crashing the interactive app or disconnecting the user's network.

After rollout, query `/api/0/organizations/dav-apps/sessions/` with the UniversalSoundboard project, `environment=production`, `groupBy=release` and fields `sum(session)`, `count_unique(user)` and `crash_free_rate(session)`. Compare only releases with session tracking and allow enough active users before interpreting changes.
