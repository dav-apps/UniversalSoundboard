using System.ComponentModel;
using UniversalSoundboard.Common;
using UniversalSoundboard.DataAccess;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Dialogs
{
    public sealed partial class UpgradePlusContentDialog : ContentDialog
    {
        string price = "";

        public UpgradePlusContentDialog()
        {
            InitializeComponent();
            UpdatePriceText();

            FileManager.itemViewHolder.PropertyChanged += ItemViewHolder_PropertyChanged;
        }

        private void ItemViewHolder_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == ItemViewHolder.UpgradePlusPriceKey)
                UpdatePriceText();
        }

        private void UpdatePriceText()
        {
            price = FileManager.loader.GetString("UpgradePlusContentDialog-PriceButtonText").Replace(
                "{0}",
                FileManager.itemViewHolder.UpgradePlusPrice
            );

            Bindings.Update();
        }
    }
}
