using System;
using System.Diagnostics;
using UniversalSoundBoard.DataAccess;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace UniversalSoundBoard.Pages
{
    public sealed partial class UpgradeDataSplashScreen : Page
    {
        internal Rect splashImageRect; // Rect to store splash screen image coordinates.
        private SplashScreen splash; // Variable to hold the splash screen object.
        internal bool dismissed = false; // Variable to track splash screen dismissal status.
        internal Frame rootFrame;

        public UpgradeDataSplashScreen(SplashScreen splashscreen, bool loadState)
        {
            InitializeComponent();

            // Listen for window resize events to reposition the extended splash screen image accordingly.
            // This ensures that the extended splash screen formats properly in response to window resizing.
            Window.Current.SizeChanged += new WindowSizeChangedEventHandler(Page_OnResize);

            splash = splashscreen;
            if (splash != null)
            {
                // Register an event handler to be executed when the splash screen has been dismissed.
                splash.Dismissed += new TypedEventHandler<SplashScreen, Object>(DismissedEventHandler);

                // Retrieve the window coordinates of the splash screen image.
                splashImageRect = splash.ImageLocation;
                PositionElements();
            }

            // Create a Frame to act as the navigation context
            rootFrame = new Frame();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            SetDataContext();
        }

        private void SetDataContext()
        {
            ContentRoot.DataContext = (App.Current as App)._itemViewHolder;
        }

        private void Page_OnResize(object sender, WindowSizeChangedEventArgs e)
        {
            // Safely update the extended splash screen image coordinates. This function will be fired in response to snapping, unsnapping, rotation, etc...
            if (splash != null)
            {
                // Update the coordinates of the splash screen image.
                splashImageRect = splash.ImageLocation;
                PositionElements();
            }
        }

        // Include code to be executed when the system has transitioned from the splash screen to the extended splash screen (application's first view).
        async void DismissedEventHandler(SplashScreen sender, object e)
        {
            dismissed = true;

            // Complete app setup operations here...
            await FileManager.MigrateToNewDataModel();
            DismissExtendedSplash();
        }

        async void DismissExtendedSplash()
        {
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                rootFrame = new Frame();
                rootFrame.Content = new MainPage(); Window.Current.Content = rootFrame;
            });
        }

        void PositionElements()
        {
            // Position the Image
            SplashScreenLogo.SetValue(Canvas.LeftProperty, splashImageRect.X);
            SplashScreenLogo.SetValue(Canvas.TopProperty, splashImageRect.Y);
            SplashScreenLogo.Height = splashImageRect.Height;
            SplashScreenLogo.Width = splashImageRect.Width;

            // Position the Progress Ring
            SplashProgressRing.SetValue(Canvas.LeftProperty, (Window.Current.Bounds.Width / 2) - (SplashProgressRing.Width / 2));
            SplashProgressRing.SetValue(Canvas.TopProperty, (Window.Current.Bounds.Height / 2) + (SplashScreenLogo.Height / 3));

            // Position the Text
            StatusStackPanel.Width = SplashScreenLogo.Width * 0.5;
            StatusStackPanel.SetValue(Canvas.LeftProperty, (Window.Current.Bounds.Width / 2) - (StatusStackPanel.Width / 2));
            StatusStackPanel.SetValue(Canvas.TopProperty, Window.Current.Bounds.Height / 2 + SplashScreenLogo.Height * 0.52);
        }
    }
}
