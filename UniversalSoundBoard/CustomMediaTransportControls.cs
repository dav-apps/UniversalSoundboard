using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundBoard
{
    public sealed class CustomMediaTransportControls : MediaTransportControls
    {
        public event EventHandler<EventArgs> Removed;

        public CustomMediaTransportControls()
        {
            this.DefaultStyleKey = typeof(CustomMediaTransportControls);
        }

        protected override void OnApplyTemplate()
        {
            // This is where you would get your custom button and create an event handler for its click method.
            Button RemoveButton = GetTemplateChild("RemoveButton") as Button;
            RemoveButton.Click += RemoveButton_Click;

            base.OnApplyTemplate();
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            // Raise an event on the custom control when 'Removed' is clicked
            Removed?.Invoke(this, EventArgs.Empty);
        }
    }
}
