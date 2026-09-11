# Browser login

Login and signup open `https://dav-apps.tech/login` or `/signup` in the default
browser. The request carries `appId`, `apiKey` and a URL-encoded `redirectUrl`:

`universalsoundboard://login?state=<random nonce>`

The existing dav-website login, signup and "login as" handlers append the
URL-encoded `accessToken` using `URL.searchParams`. They preserve the state query
parameter, including when switching between login and signup. No website change
is required. The packaging manifest already registers `universalsoundboard`.

The app accepts a callback only for its pending nonce, within ten minutes, and
consumes it once. Correlation data (not the returned token) is saved in local
settings so a callback can also start a terminated app. Existing callers await
actual login completion before continuing, including the Plus checkout flow.
A new attempt supersedes the old one. Closing the browser cannot notify the app;
the attempt expires after ten minutes, and the user can start another login.
Callback URLs and access tokens must never be included in telemetry.

## Validation

Run `dotnet run --project UniversalSoundboard.Tests/Regression/Regression.csproj`
for callback validation and URL encoding tests. Build the package with
`powershell -NoProfile -ExecutionPolicy Bypass -File build.ps1 -Configuration Debug -Platform x64`.

Manual checks with the installed package and a test account:

1. Open Account → Login. Verify the default browser opens the current dav site.
2. Sign in and accept the browser's prompt to open UniversalSoundboard. Verify
   the account and sync update. Repeat for signup and an existing browser session.
3. Start login, close the app, then complete login. Verify the app starts and syncs.
4. Start another attempt before completing the first. The old callback must not
   log in; the current attempt should work. Replaying a completed callback or
   opening a callback without first starting login must have no effect.
5. Close the browser without signing in and retry from the app.
6. Start login from the Plus dialog and verify checkout continues only after
   login and user sync; also check login from sound publishing/download flows.

The full browser round trip requires an interactive account login; the regression
suite uses synthetic callbacks and does not authenticate against the live service.
