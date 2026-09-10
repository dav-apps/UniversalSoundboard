# Diagnostics and purchase measurement

Sentry releases use the installed package version (`UniversalSoundboard@major.minor.build.revision`). Debug builds use `debug`, release builds use `production`. Filter by environment when measuring usage.

## Audio failures

Audio initialization is serialized. A failure invalidates initialization state and forces the next attempt to rebuild the graph. Playback does not continue after a failed initialization; pausing an uninitialized player cancels its resume intent without trying to initialize it again.

Audio exceptions carry `audio.stage` and `audio.status`. Annotated player failures also include `audio.output_count`, `audio.file_type`, and `audio.hresult`. No file path or audio-device name is added by this instrumentation.

The Sentry before-send processor retains `audio.exception_details` (the original exception and inner exceptions) and a separately labelled `audio.capture_stack`. The latter is the capture site, not the throw site. Available frame names and source lines still depend on runtime metadata and symbols, especially in the Native AOT audio component. Generated symbol packages are not automatically uploaded to Sentry by this change. The application itself uses self-contained .NET 10; see [the migration notes](dotnet-migration.md).

## Plus events

Events use `purchase.source`, `purchase.method`, `purchase.attempt_id`, `purchase.outcome`, and optional `purchase.error_code`. Each click has its own attempt ID. `purchase.offer_id` connects clicks to the displayed offer. Attempts are not sessions or unique users.

| Event | Meaning |
| --- | --- |
| `Plus-OfferViewed` / `Plus-OfferClosed` | The dialog actually opened / closed, not just constructed or queued. |
| `Plus-PurchaseClicked` | A one-time or subscription purchase button was clicked. |
| `Plus-LoginStarted` / `Plus-LoginResult` | Subscription login, distinguishing authentication status and exceptions. |
| `Plus-UserSyncResult` | User data finished syncing after login, or the wait timed out. |
| `Plus-CheckoutStarted` | Before calling the Store or creating a subscription checkout session. |
| `Plus-CheckoutResult` | Full Store status and extended HRESULT, or subscription session failure / exception. |
| `Plus-CheckoutSessionCreated` | The subscription API returned a usable checkout URL. |
| `Plus-CheckoutBrowserResult` | The browser accepted or rejected launching checkout. |
| `Plus-PurchaseCompleted` | The Microsoft Store returned `Succeeded`. |
| `Plus-PurchaseRestored` | The Store returned `AlreadyPurchased`; not a new sale. |
| `Plus-CheckoutReturn` | An external subscription callback returned; not proof of payment. |
| `Plus-EntitlementVerified` | Account synchronization confirmed an active/inactive plan, or failed. An active plan is not necessarily a new sale. |
| `Plus-EntitlementAlreadyActive` | Login revealed an existing paid plan; no checkout needed. |

Dialog sources are `output_device`, `output_devices_manage`, `playing_sound_output_device`, `hotkey_settings`, and `hotkey_pressed`. The account page uses `account_page`.

The existing `UpgradePlusDialog-PurchasePlus` event is retained with `succeeded` and the full `status`. Do not add its count to the new checkout-result count. `UpgradeSuccessful` is replaced by the explicit callback/entitlement events. The misleading `UpgradePlusDialog-UpgradePlusButtonClick` is replaced by `Plus-PurchaseClicked` with method `subscription`.

The current website callback does not include an attempt ID, so return events deliberately omit it. Closing a browser without returning cannot be observed by the app. Counting new subscription payments reliably requires payment-provider/backend data; this client change does not claim to provide that.

## URL lookup

YouTube links are parsed as URIs, with exact host and 11-character video-ID validation. Short links, watch, shorts, live, embed, share parameters and video-associated playlists are supported. Playlist-only URLs remain unsupported. Input is debounced by 400 ms; superseded YouTube requests are cancelled, and stale results from every provider are prevented from updating the dialog.

## Regression checks

With the .NET 10 SDK installed:

```powershell
dotnet run --project UniversalSoundboard.Tests/Regression/Regression.csproj
```

These checks link the actual URL parser, async-operation gate, and Sentry audio enrichment. They test short links with share parameters, partial/invalid input, host validation, playlist preservation, concurrent initialization, recovery after failure, and exception diagnostic fields. They do not initialize Sentry or send test events. Live Store payments, audio hardware changes, and Native AOT symbolication require separate integration verification.

Local Sentry API credentials belong outside compiled source. The analysis token is stored in the ignored `.review/sentry-token.txt`; it is not needed by the app.
