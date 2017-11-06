using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using UniversalSoundBoard.Model;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace UniversalSoundBoard
{
    public sealed partial class SoundItemTemplate : UserControl
    {
        public Sound Sound { get { return this.DataContext as Sound; } }

        public SoundItemTemplate()
        {
            this.InitializeComponent();
            this.DataContextChanged += (s, e) => Bindings.Update();

            if(App.Current.RequestedTheme == ApplicationTheme.Dark)
                RemoveSoundButton.Background = new SolidColorBrush(Colors.Black);
            else
                RemoveSoundButton.Background = new SolidColorBrush(Colors.White);
        }

        private void RemoveSoundButton_Click(object sender, RoutedEventArgs e)
        {
            ContentDialogs.SoundsList.Remove(Sound);

            if(ContentDialogs.SoundsList.Count() == 0)
            {
                ContentDialogs.PlaySoundsSuccessivelyContentDialog.IsPrimaryButtonEnabled = false;
            }
        }
    }
}
