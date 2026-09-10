using davClassLibrary;
using davClassLibrary.Controllers;
using Sentry;
using System;
using System.Threading.Tasks;
using UniversalSoundboard.DataAccess;
using Windows.System;

namespace UniversalSoundboard.Common
{
    internal static class PurchaseTelemetry
    {
        public static void Track(string name, string source, string method, string attemptId,
            string outcome = null, string errorCode = null, string offerId = null)
        {
            SentrySdk.CaptureMessage(name, scope =>
            {
                scope.SetTag("purchase.source", source);
                scope.SetTag("purchase.method", method);
                if (attemptId != null) scope.SetTag("purchase.attempt_id", attemptId);
                scope.SetTag("purchase.logged_in", Dav.IsLoggedIn.ToString());
                scope.SetTag("purchase.plus_active", FileManager.IsUserOnPlus().ToString());
                if (outcome != null) scope.SetTag("purchase.outcome", outcome);
                if (errorCode != null) scope.SetTag("purchase.error_code", errorCode);
                if (offerId != null) scope.SetTag("purchase.offer_id", offerId);
            });
        }

        public static async Task HandleSubscriptionReturnAsync(Uri uri)
        {
            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            bool cancelled = string.Equals(query.Get("success"), "false", StringComparison.OrdinalIgnoreCase);
            // The callback carries no attempt ID. A browser return is not a payment confirmation.
            Track("Plus-CheckoutReturn", "checkout_return", "subscription", null,
                cancelled ? "cancelled" : "returned");
            if (cancelled || !Dav.IsLoggedIn) return;
            try
            {
                bool synced = await davClassLibrary.DataAccess.SyncManager.UserSync();
                bool active = synced && Dav.User.Plan > 0;
                Track("Plus-EntitlementVerified", "checkout_return", "subscription", null,
                    !synced ? "sync_failed" : active ? "active" : "inactive");
                if (active) FileManager.itemViewHolder.TriggerUserPlanChangedEvent(null, EventArgs.Empty);
            }
            catch (Exception exception)
            {
                Track("Plus-EntitlementVerified", "checkout_return", "subscription", null, "exception",
                    $"{exception.GetType().Name}:0x{exception.HResult:X8}");
            }
        }

        public static async Task<bool> StartSubscriptionCheckoutAsync(string source, string attemptId)
        {
            Track("Plus-CheckoutStarted", source, "subscription", attemptId);
            try
            {
                var response = await CheckoutSessionsController.CreateSubscriptionCheckoutSession(
                    "url", Plan.Plus, Constants.CreateCheckoutSessionSuccessUrl, Constants.CreateCheckoutSessionCancelUrl);
                if (response == null || !response.Success || response.Data == null
                    || !Uri.TryCreate(response.Data.url, UriKind.Absolute, out var checkoutUri))
                {
                    Track("Plus-CheckoutResult", source, "subscription", attemptId, "session_failed");
                    return false;
                }
                Track("Plus-CheckoutSessionCreated", source, "subscription", attemptId);
                bool launched = await Launcher.LaunchUriAsync(checkoutUri);
                Track("Plus-CheckoutBrowserResult", source, "subscription", attemptId,
                    launched ? "opened" : "launch_failed");
                return launched;
            }
            catch (Exception exception)
            {
                // Never log the checkout URL or authentication response.
                Track("Plus-CheckoutResult", source, "subscription", attemptId, "exception",
                    $"{exception.GetType().Name}:0x{exception.HResult:X8}");
                return false;
            }
        }
    }
}
