using davClassLibrary;
using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using UniversalSoundboard.DataAccess;
using Windows.Storage;
using Windows.System;

namespace UniversalSoundboard.Common
{
    internal static class BrowserLogin
    {
        private const string PendingKey = "PendingBrowserLogin";
        private static TaskCompletionSource<string> pending;

        public static async Task<bool> StartAsync(bool signup, Action<string> reportOutcome)
        {
            // A retry supersedes the previous browser tab and releases its waiting caller.
            pending?.TrySetResult("Superseded");
            var completion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            pending = completion;
            string state = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
            string outcome;
            try
            {
                ApplicationData.Current.LocalSettings.Values[PendingKey] = new ApplicationDataCompositeValue
                {
                    ["state"] = state,
                    ["started"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };
                var request = BrowserLoginProtocol.CreateRequest(Constants.WebsiteBaseUrl,
                    Constants.AppId, Constants.ApiKey, signup, state);
                if (!await Launcher.LaunchUriAsync(request)) outcome = "BrowserLaunchFailed";
                else
                {
                    var finished = await Task.WhenAny(completion.Task, Task.Delay(BrowserLoginProtocol.Lifetime));
                    outcome = finished == completion.Task ? await completion.Task : "Timeout";
                }
            }
            catch (Exception exception)
            {
                // Do not log the callback URI or token.
                outcome = "Exception:" + exception.GetType().Name;
            }
            finally
            {
                if (pending == completion)
                {
                    pending = null;
                    ApplicationData.Current.LocalSettings.Values.Remove(PendingKey);
                }
            }
            reportOutcome?.Invoke(outcome);
            return outcome == "Success";
        }

        public static void HandleCallback(Uri uri)
        {
            try
            {
                // Persist only correlation data so returning after process termination also works.
                var saved = ApplicationData.Current.LocalSettings.Values[PendingKey] as ApplicationDataCompositeValue;
                if (saved == null || !(saved["started"] is long started)
                    || !BrowserLoginProtocol.TryGetToken(uri, saved["state"] as string,
                        DateTimeOffset.FromUnixTimeSeconds(started), DateTimeOffset.UtcNow, out var token)) return;

                // Consume before logging in: duplicate callbacks cannot change the account again.
                ApplicationData.Current.LocalSettings.Values.Remove(PendingKey);
                ApiManager.ReloadClients(token);
                FileManager.itemViewHolder.TriggerShowInAppNotificationEvent(null,
                    new ShowInAppNotificationEventArgs(InAppNotificationType.Sync,
                        FileManager.loader.GetString("InAppNotification-Sync"), 0, true));
                if (FileManager.itemViewHolder.AllSounds.Count == 0)
                    FileManager.itemViewHolder.AppState = AppState.InitialSync;
                Dav.Login(token);
                pending?.TrySetResult("Success");
            }
            catch (Exception exception)
            {
                pending?.TrySetResult("Exception:" + exception.GetType().Name);
            }
        }
    }
}
