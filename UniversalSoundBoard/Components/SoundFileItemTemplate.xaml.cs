using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundboard.Components
{
    public sealed partial class SoundFileItemTemplate : UserControl
    {
        public SoundFileItem SoundFileItem { get { return DataContext as SoundFileItem; } }

        public SoundFileItemTemplate()
        {
            InitializeComponent();
            DataContextChanged += (s, e) => Bindings.Update();
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            SoundFileItem.TriggerRemovedEvent(new EventArgs());
        }
    }
}
