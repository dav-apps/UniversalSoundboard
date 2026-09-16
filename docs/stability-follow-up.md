# Production follow-up: 2.5.10 (2026-09-16)

The production review found 198 error events across 19 installation IDs in seven days. Of these, 188 were marked handled; 10 unhandled events affected four installation IDs. These counts are not session-based crash rates.

## Changes

- **VQ / VZ, audio connections:** Both groups report `ConnectNodes` with `0x88970001` (unsupported effect format). Audio graphs now explicitly use 48 kHz, stereo, 32-bit float internally instead of inheriting the output device's mix format. This is supported by the attached effect chain; file nodes convert source audio into the graph format. Connection failures include the actual graph sample rate, channel count and subtype in Sentry. Source formats and the selected output device are preserved; processing is intentionally limited to stereo/48 kHz.
- **VC, pausing:** The position timer is initialized before asynchronous device/player initialization. A pause delivered while initialization is pending no longer accesses a missing timer.
- **W7 / W8 / VB, sound store:** Every home-page section observes and handles its load task. Home and search pages stop their spinners on failure and offer a localized retry. Null responses do not become null-reference exceptions. Search responses are ignored if a newer search has started; failed pagination retries the same page.
- **VR / VY, synchronization (davClassLibrary):** File-update calls had MIME type and file path swapped. Both upload call sites now use named arguments. Missing or unreadable files fail without escaping into the async application startup path. Failed transport responses without an error object no longer cause a second null-reference exception. New file objects without a local file are not submitted as successful uploads.
- **Purchase login:** Closing the offer cancels its pending browser-login wait and reports `Plus-LoginResult: Cancelled`. Browser launch gets its own `Plus-LoginBrowserOpened` event. Successful callbacks, cancellation and timeout remain distinct. No payment flow or entitlement decision is inferred from browser launch alone.

## Verification and limits

- x64 Debug and Release package builds passed. The updated Debug app loaded the store sections and returned results for the `piano` tag in UI automation. Offline retry and stale-response paths were reviewed in code; no network outage was forced on the live desktop.
- Standalone app regression suite: 148 checks passed, including browser callback validation and success/cancellation/timeout behavior.
- davClassLibrary focused suite: three tests passed. A fake HTTP handler verifies that the update sends the actual file bytes with `image/png`, then simulates a transport failure with no API error object. Missing-file and authenticated/public-download behavior are also covered.
- Native audio probe under the development package identity: file inputs at 22.05/44.1/48/96 kHz with 1/2/6 channels initialized and supported play/pause and 1.25x speed. Synthetic input is silence; this is not a listening test.
- The full-effect format probe failed at 96/192 kHz with `0x88960001`, while 44.1/48 kHz passed. Using the application's new fixed graph format passed the connection probe. The exact production `0x88970001` could not be reproduced on the available device; verify VQ/VZ after rollout rather than treating them as conclusively resolved across all drivers.
- The single production purchase attempt only showed login start. That alone does not establish why its callback never arrived. Closing the dialog is now observable; process termination and an actual successful paid checkout still require separate end-to-end validation.

Run the app checks with `dotnet run --project UniversalSoundboard.Tests/Regression/Regression.csproj`.
Run the library checks with `dotnet test ../davClassLibrary/davClassLibrary.Tests/davClassLibrary.Tests.csproj --filter "FullyQualifiedName~UploadFailureTest|FullyQualifiedName~ApiManagerTest"`.
