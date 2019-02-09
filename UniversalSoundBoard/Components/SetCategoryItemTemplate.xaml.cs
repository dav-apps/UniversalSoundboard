using UniversalSoundBoard.Common;
using UniversalSoundBoard.Models;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Components
{
    public sealed partial class SetCategoryItemTemplate : UserControl
    {
        public Category Category { get { return DataContext as Category; } }

        public SetCategoryItemTemplate()
        {
            InitializeComponent();
            DataContextChanged += (s, e) => Bindings.Update();
        }

        private void UserControl_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            SetCheckboxState();
        }

        private void SetCheckboxState()
        {
            SetCategoryCheckbox.IsChecked = ContentDialogs.SelectedCategories[Category.Uuid] == true;
        }

        private void SetCategoryCheckbox_Checked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            UpdateInSelectedCategories(true);
        }

        private void SetCategoryCheckbox_Unchecked(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            UpdateInSelectedCategories(false);
        }

        private void UpdateInSelectedCategories(bool value)
        {
            // Update the value in the SelectedCategories Dictionary in ContentDialogs
            ContentDialogs.SelectedCategories[Category.Uuid] = value;
        }
    }
}
