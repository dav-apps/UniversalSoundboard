using UniversalSoundboard.Models;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Components
{
    public sealed partial class DialogSoundListItemTemplate : UserControl
    {
        public DialogSoundListItem DialogSoundListItem { get => DataContext as DialogSoundListItem; }
        public Sound Sound { get => DialogSoundListItem?.Sound; }
        
        public DialogSoundListItemTemplate()
        {
            InitializeComponent();
            DataContextChanged += (s, e) => Bindings.Update();
        }

        private void RemoveSoundButton_Click(object sender, RoutedEventArgs e)
        {
            DialogSoundListItem?.TriggerRemoveButtonClickEvent();
        }
    }
}
