using UniversalSoundboard.Models;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace UniversalSoundboard.Pages
{
    public sealed partial class StoreSoundPage : Page
    {
        private string name = "";

        public StoreSoundPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            var soundItem = e.Parameter as SoundResponse;
            name = soundItem.Name;
            Bindings.Update();
        }
    }
}
