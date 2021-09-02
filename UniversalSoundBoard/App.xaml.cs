using davClassLibrary;
using davClassLibrary.Common;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using UniversalSoundboard.Common;
using UniversalSoundboard.DataAccess;
using UniversalSoundboard.Pages;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace UniversalSoundboard
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        public static BackgroundTaskDeferral AppServiceDeferral = null;
        public static AppServiceConnection Connection = null;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
            Suspending += OnSuspending;

            // Init AppCenter
            AppCenter.Start(Env.AppCenterSecretKey, typeof(Analytics), typeof(Crashes));

            Crashes.GetErrorAttachments = (ErrorReport report) =>
            {
                // Collect settings
                string settings = "";
                settings += $"playingSoundsListVisible: {FileManager.itemViewHolder.PlayingSoundsListVisible}\n";
                settings += $"savePlayingSounds: {FileManager.itemViewHolder.SavePlayingSounds}\n";
                settings += $"openMultipleSounds: {FileManager.itemViewHolder.OpenMultipleSounds}\n";
                settings += $"multiSoundPlayback: {FileManager.itemViewHolder.MultiSoundPlayback}\n";
                settings += $"showSoundsPivot: {FileManager.itemViewHolder.ShowSoundsPivot}\n";
                settings += $"soundOrder: {FileManager.itemViewHolder.SoundOrder}\n";
                settings += $"soundOrderReversed: {FileManager.itemViewHolder.SoundOrderReversed}\n";
                settings += $"showListView: {FileManager.itemViewHolder.ShowListView}\n";
                settings += $"showCategoriesIcons: {FileManager.itemViewHolder.ShowCategoriesIcons}\n";
                settings += $"showAcrylicBackground: {FileManager.itemViewHolder.ShowAcrylicBackground}\n";
                settings += $"isLoggedIn: {Dav.IsLoggedIn}";

                return new ErrorAttachmentLog[]
                {
                    ErrorAttachmentLog.AttachmentWithText(settings, "settings.txt")
                };
            };

            // Init Websocket
            Websockets.Net.WebsocketConnection.Link();

            // Initialize Dav settings
            ProjectInterface.LocalDataSettings = new LocalDataSettings();
            ProjectInterface.Callbacks = new Callbacks();

            Dav.Init(
                FileManager.Environment,
                FileManager.AppId,
                new List<int>
                {
                    FileManager.OrderTableId,
                    FileManager.CategoryTableId,
                    FileManager.SoundFileTableId,
                    FileManager.SoundTableId,
                    FileManager.PlayingSoundTableId,
                    FileManager.ImageFileTableId
                },
                new List<int>
                {
                    FileManager.SoundFileTableId,
                    FileManager.SoundTableId
                },
                FileManager.GetDavDataPath()
            );

            // Init itemViewHolder
            FileManager.itemViewHolder = new ItemViewHolder();

            // Set the theme
            if (FileManager.itemViewHolder.Theme == FileManager.AppTheme.Light)
                FileManager.itemViewHolder.CurrentTheme = FileManager.AppTheme.Light;
            else if (FileManager.itemViewHolder.Theme == FileManager.AppTheme.Dark)
                FileManager.itemViewHolder.CurrentTheme = FileManager.AppTheme.Dark;
            else
                FileManager.itemViewHolder.CurrentTheme = RequestedTheme == ApplicationTheme.Light ? FileManager.AppTheme.Light : FileManager.AppTheme.Dark;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (e.PrelaunchActivated == false)
            {
                if (rootFrame.Content == null)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.ActualThemeChanged += RootFrame_ActualThemeChanged;
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // Ensure the current window is active
                Window.Current.Activate();
            }
            
            // Check if app was launched from a secondary tile
            if (!string.IsNullOrEmpty(e.Arguments))
            {
                Guid? soundUuid = FileManager.ConvertStringToGuid(e.Arguments);
                if(soundUuid.HasValue)
                    await SoundPage.PlaySoundAfterPlayingSoundsLoadedAsync(await FileManager.GetSoundAsync(soundUuid.Value));
            }

            Window.Current.Activate();
        }

        private void RootFrame_ActualThemeChanged(FrameworkElement sender, object args)
        {
            if (FileManager.itemViewHolder.Theme != FileManager.AppTheme.System) return;

            // Update the theme
            FileManager.itemViewHolder.CurrentTheme = sender.ActualTheme == ElementTheme.Dark ? FileManager.AppTheme.Dark : FileManager.AppTheme.Light;
        }

        private Frame CreateRootFrame(ApplicationExecutionState previousExecutionState, string arguments, Type page)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                Debug.WriteLine("CreateFrame: Initializing root frame ...");

                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                // Set the default language
                rootFrame.Language = Windows.Globalization.ApplicationLanguages.Languages[0];

                rootFrame.NavigationFailed += OnNavigationFailed;

                if (previousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            return rootFrame;
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }

        protected override void OnBackgroundActivated(BackgroundActivatedEventArgs args)
        {
            base.OnBackgroundActivated(args);

            if (
                args.TaskInstance.TriggerDetails is AppServiceTriggerDetails details
                && details.CallerPackageFamilyName == Package.Current.Id.FamilyName
            )
            {
                // Connection established from the fulltrust process
                AppServiceDeferral = args.TaskInstance.GetDeferral();
                Connection = details.AppServiceConnection;

                AppServiceTriggerDetails triggerDetails = (args.TaskInstance.TriggerDetails as AppServiceTriggerDetails);
                triggerDetails.AppServiceConnection.RequestReceived += AppServiceConnection_RequestReceived;
            }
        }

        private async void AppServiceConnection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            AppServiceDeferral messageDeferral = args.GetDeferral();

            int id = (int)args.Request.Message["id"];
            await FileManager.HandleHotkeyPressed(id);

            await args.Request.SendResponseAsync(new ValueSet());
            messageDeferral.Complete();

            // Close the connection
            AppServiceDeferral.Complete();
            Connection = null;
        }

        protected override void OnShareTargetActivated(ShareTargetActivatedEventArgs args)
        {
            if (args.PreviousExecutionState == ApplicationExecutionState.NotRunning)
            {
                ShareOperation shareOperation = args.ShareOperation;
                if (shareOperation.Data.Contains(StandardDataFormats.StorageItems))
                {
                    shareOperation.ReportDataRetrieved();

                    Frame frame = new Frame();
                    frame.Navigate(typeof(ShareTargetPage), shareOperation);

                    Window.Current.Content = frame;
                    Window.Current.Activate();
                }
                else
                {
                    shareOperation.ReportError("An error occured.");
                }
            }
            else
            {
                // App is running
                ShareOperation shareOperation = args.ShareOperation;
                if (shareOperation.Data.Contains(StandardDataFormats.StorageItems))
                {
                    shareOperation.ReportDataRetrieved();

                    var rootFrame = CreateRootFrame(args.PreviousExecutionState, "", typeof(ShareTargetPage));
                    rootFrame.Navigate(typeof(ShareTargetPage), shareOperation);
                    Window.Current.Activate();
                }
                else
                {
                    shareOperation.ReportError("An error occured.");
                }
            }
        }
    }
}
