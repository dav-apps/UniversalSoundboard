using davClassLibrary;
using davClassLibrary.Common;
using Microsoft.AppCenter.Analytics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Web;
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
using Windows.Storage;
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
            if (FileManager.itemViewHolder.Theme == AppTheme.Light)
                FileManager.itemViewHolder.CurrentTheme = AppTheme.Light;
            else if (FileManager.itemViewHolder.Theme == AppTheme.Dark)
                FileManager.itemViewHolder.CurrentTheme = AppTheme.Dark;
            else
                FileManager.itemViewHolder.CurrentTheme = RequestedTheme == ApplicationTheme.Light ? AppTheme.Light : AppTheme.Dark;
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
                if (soundUuid.HasValue)
                {
                    FileManager.itemViewHolder.TriggerPlaySoundAfterPlayingSoundsLoadedEvent(
                        this,
                        new PlaySoundAfterPlayingSoundsLoadedEventArgs(
                            await FileManager.GetSoundAsync(soundUuid.Value)
                        )
                    );
                }
            }

            Window.Current.Activate();
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            if (args.Kind == ActivationKind.Protocol)
            {
                Frame rootFrame = Window.Current.Content as Frame;
                ProtocolActivatedEventArgs eventArgs = args as ProtocolActivatedEventArgs;

                // Create the root frame if the app is not already running
                if (rootFrame == null)
                {
                    rootFrame = new Frame();
                    rootFrame.NavigationFailed += OnNavigationFailed;
                    rootFrame.ActualThemeChanged += RootFrame_ActualThemeChanged;

                    rootFrame.Navigate(typeof(MainPage));

                    Window.Current.Content = rootFrame;
                }

                Window.Current.Activate();
                
                if (eventArgs.Uri.AbsoluteUri.StartsWith("universalsoundboard://upgrade"))
                {
                    var queryDictionary = HttpUtility.ParseQueryString(eventArgs.Uri.Query);
                    string planParam = queryDictionary.Get("plan");

                    if (planParam == "1" || planParam == "2")
                    {
                        // Upgrade the plan
                        if (planParam == "1")
                            Dav.User.Plan = Plan.Plus;
                        else if (planParam == "2")
                            Dav.User.Plan = Plan.Pro;

                        FileManager.itemViewHolder.TriggerUserPlanChangedEvent(this, new EventArgs());
                        Analytics.TrackEvent("UpgradeSuccessful");
                    }
                }
                else if (eventArgs.Uri.AbsoluteUri.StartsWith("universalsoundboard://sound-promotion"))
                {
                    FileManager.itemViewHolder.TriggerSoundPromotionStartedEvent(this, EventArgs.Empty);
                }
            }
        }

        protected override void OnFileActivated(FileActivatedEventArgs args)
        {
            if (args.Files.Count == 0) return;

            // Handle file activation
            Frame rootFrame = Window.Current.Content as Frame;
            
            // Create the root frame if the app is not already running
            if (rootFrame == null)
            {
                rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;
                rootFrame.ActualThemeChanged += RootFrame_ActualThemeChanged;

                rootFrame.Navigate(typeof(MainPage));

                Window.Current.Content = rootFrame;
            }

            Window.Current.Activate();

            // Start playing the local sound file
            var item = args.Files[0] as StorageFile;

            if (SoundPage.PlayingSoundsLoaded)
            {
                FileManager.itemViewHolder.TriggerPlayLocalSoundAfterPlayingSoundsLoadedEvent(
                    this,
                    new PlayLocalSoundAfterPlayingSoundsLoadedEventArgs(item)
                );
            }
            else
            {
                SoundPage.LocalSoundsToPlayAfterPlayingSoundsLoaded.Add(item);
            }
        }

        private void RootFrame_ActualThemeChanged(FrameworkElement sender, object args)
        {
            if (FileManager.itemViewHolder.Theme != AppTheme.System) return;

            // Update the theme
            FileManager.itemViewHolder.CurrentTheme = sender.ActualTheme == ElementTheme.Dark ? AppTheme.Dark : AppTheme.Light;
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

            await MainPage.dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
            {
                await FileManager.HandleHotkeyPressed(id);
            });

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
