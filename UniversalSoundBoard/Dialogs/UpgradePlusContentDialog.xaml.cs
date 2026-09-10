using davClassLibrary;
using Sentry;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using UniversalSoundboard.Common;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Pages;
using Windows.Services.Store;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Dialogs
{
    public sealed partial class UpgradePlusContentDialog : ContentDialog
    {
        private readonly string source;
        private readonly string offerId = Guid.NewGuid().ToString();
        private bool isBusy;
        private bool isClosed;
        private readonly TaskCompletionSource<bool> closed = new TaskCompletionSource<bool>();
        string price = "";

        public event EventHandler<EventArgs> UpgradePlusSucceeded;

        public UpgradePlusContentDialog(string source = "unknown")
        {
            this.source = source;
            InitializeComponent();
            UpdatePriceText();
            Opened += OnOpened;
            Closed += OnClosed;
        }

        private void OnOpened(ContentDialog sender, ContentDialogOpenedEventArgs args)
        {
            FileManager.itemViewHolder.PropertyChanged += ItemViewHolder_PropertyChanged;
            UpdatePriceText();
            PurchaseTelemetry.Track("Plus-OfferViewed", source, "choice", offerId, offerId: offerId);
        }

        private void OnClosed(ContentDialog sender, ContentDialogClosedEventArgs args)
        {
            isClosed = true;
            closed.TrySetResult(true);
            FileManager.itemViewHolder.PropertyChanged -= ItemViewHolder_PropertyChanged;
            PurchaseTelemetry.Track("Plus-OfferClosed", source, "choice", offerId, offerId: offerId);
        }

        private void ItemViewHolder_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == ItemViewHolder.UpgradePlusPriceKey) UpdatePriceText();
        }

        private void UpdatePriceText()
        {
            price = FileManager.loader.GetString("UpgradePlusContentDialog-PriceButtonText")
                .Replace("{0}", FileManager.itemViewHolder.UpgradePlusPrice);
            Bindings.Update();
        }

        private void SetBusy(bool busy)
        {
            isBusy = busy;
            UpgradePlusButton.IsEnabled = DavPlusButton.IsEnabled = !busy;
        }

        private async void UpgradePlusButton_Click(object sender, RoutedEventArgs e)
        {
            if (isBusy) return;
            SetBusy(true);
            string attemptId = Guid.NewGuid().ToString();
            PurchaseTelemetry.Track("Plus-PurchaseClicked", source, "one_time", attemptId, offerId: offerId);
            PurchaseTelemetry.Track("Plus-CheckoutStarted", source, "one_time", attemptId);
            try
            {
                var result = await StoreContext.GetDefault().RequestPurchaseAsync(Constants.UniversalSoundboardPlusAddonStoreId);
                PurchaseTelemetry.Track("Plus-CheckoutResult", source, "one_time", attemptId,
                    result.Status.ToString(), result.ExtendedError == null ? null : $"0x{result.ExtendedError.HResult:X8}");
                SentrySdk.CaptureMessage("UpgradePlusDialog-PurchasePlus", scope =>
                {
                    scope.SetTag("succeeded", (result.Status == StorePurchaseStatus.Succeeded).ToString());
                    scope.SetTag("status", result.Status.ToString());
                });
                if (result.Status == StorePurchaseStatus.Succeeded || result.Status == StorePurchaseStatus.AlreadyPurchased)
                {
                    PurchaseTelemetry.Track(result.Status == StorePurchaseStatus.Succeeded
                        ? "Plus-PurchaseCompleted" : "Plus-PurchaseRestored", source, "one_time", attemptId);
                    UpgradePlusSucceeded?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception exception)
            {
                PurchaseTelemetry.Track("Plus-CheckoutResult", source, "one_time", attemptId, "exception",
                    $"{exception.GetType().Name}:0x{exception.HResult:X8}");
            }
            finally { SetBusy(false); }
        }

        private async void DavPlusButton_Click(object sender, RoutedEventArgs e)
        {
            if (isBusy) return;
            SetBusy(true);
            string attemptId = Guid.NewGuid().ToString();
            PurchaseTelemetry.Track("Plus-PurchaseClicked", source, "subscription", attemptId, offerId: offerId);
            try
            {
                if (!Dav.IsLoggedIn)
                {
                    var synced = new TaskCompletionSource<bool>();
                    EventHandler<EventArgs> onSync = (s, args) =>
                    {
                        if (Dav.IsLoggedIn) synced.TrySetResult(true);
                    };
                    FileManager.itemViewHolder.UserSyncFinished += onSync;
                    try
                    {
                        PurchaseTelemetry.Track("Plus-LoginStarted", source, "subscription", attemptId);
                        bool loggedIn = await AccountPage.ShowLoginPage(false, outcome =>
                            PurchaseTelemetry.Track("Plus-LoginResult", source, "subscription", attemptId, outcome));
                        if (!loggedIn || isClosed) return;
                        var completed = await Task.WhenAny(synced.Task, closed.Task, Task.Delay(TimeSpan.FromSeconds(30)));
                        if (isClosed) return;
                        bool syncSucceeded = completed == synced.Task;
                        PurchaseTelemetry.Track("Plus-UserSyncResult", source, "subscription", attemptId,
                            syncSucceeded ? "completed" : "timeout");
                        if (!syncSucceeded) return;
                    }
                    finally { FileManager.itemViewHolder.UserSyncFinished -= onSync; }
                }
                if (isClosed) return;
                if (Dav.User.Plan > 0)
                {
                    PurchaseTelemetry.Track("Plus-EntitlementAlreadyActive", source, "subscription", attemptId);
                    Hide();
                }
                else
                {
                    await PurchaseTelemetry.StartSubscriptionCheckoutAsync(source, attemptId);
                }
            }
            catch (Exception exception)
            {
                PurchaseTelemetry.Track("Plus-CheckoutResult", source, "subscription", attemptId, "exception",
                    $"{exception.GetType().Name}:0x{exception.HResult:X8}");
            }
            finally { SetBusy(false); }
        }
    }
}
