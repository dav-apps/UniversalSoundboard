using davClassLibrary;
using davClassLibrary.Controllers;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using UniversalSoundboard.Common;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Pages;
using Windows.Services.Store;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Dialogs
{
    public sealed partial class UpgradePlusContentDialog : ContentDialog
    {
        private bool loginSuccessful = false;
        string price = "";

        public event EventHandler<EventArgs> UpgradePlusSucceeded;

        public UpgradePlusContentDialog()
        {
            InitializeComponent();
            UpdatePriceText();

            FileManager.itemViewHolder.PropertyChanged += ItemViewHolder_PropertyChanged;
            FileManager.itemViewHolder.UserSyncFinished += ItemViewHolder_UserSyncFinished;
        }

        private void ItemViewHolder_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == ItemViewHolder.UpgradePlusPriceKey)
                UpdatePriceText();
        }

        private async void ItemViewHolder_UserSyncFinished(object sender, EventArgs e)
        {
            if (!loginSuccessful) return;

            if (Dav.User.Plan == 0)
                await NavigateToCheckout();
            else
                Hide();
        }

        private void UpdatePriceText()
        {
            price = FileManager.loader.GetString("UpgradePlusContentDialog-PriceButtonText").Replace(
                "{0}",
                FileManager.itemViewHolder.UpgradePlusPrice
            );

            Bindings.Update();
        }

        private async void UpgradePlusButton_Click(object sender, RoutedEventArgs e)
        {
            var context = StoreContext.GetDefault();
            StorePurchaseResult result = await context.RequestPurchaseAsync(Constants.UniversalSoundboardPlusAddonStoreId);
            
            if (result.Status == StorePurchaseStatus.Succeeded)
                UpgradePlusSucceeded?.Invoke(this, new EventArgs());
        }

        private async void DavPlusButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Dav.IsLoggedIn)
            {
                loginSuccessful = await AccountPage.ShowLoginPage();
            }
            else if (Dav.User.Plan == 0)
            {
                DavPlusButton.IsEnabled = false;
                await NavigateToCheckout();
                DavPlusButton.IsEnabled = true;
            }
        }

        private async Task NavigateToCheckout()
        {
            var createCheckoutSessionResponse = await CheckoutSessionsController.CreateCheckoutSession(
                    1,
                    Constants.CreateCheckoutSessionSuccessUrl,
                    Constants.CreateCheckoutSessionCancelUrl
                );

            if (createCheckoutSessionResponse.Success)
                await Launcher.LaunchUriAsync(new Uri(createCheckoutSessionResponse.Data.SessionUrl));
        }
    }
}
