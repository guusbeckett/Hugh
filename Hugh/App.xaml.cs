using Hugh.Services;
using Hugh.Views_Viewmodels;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace Hugh
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public sealed partial class App : Application
    {
        private TransitionCollection transitions;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += this.OnSuspending;
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used when the application is launched to open a specific file, to display
        /// search results, and so forth.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif

            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                // TODO: change this value to a cache size that is appropriate for your application
                rootFrame.CacheSize = 1;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    // TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                // Removes the turnstile navigation for startup.
                if (rootFrame.ContentTransitions != null)
                {
                    this.transitions = new TransitionCollection();
                    foreach (var c in rootFrame.ContentTransitions)
                    {
                        this.transitions.Add(c);
                    }
                }

                rootFrame.ContentTransitions = null;
                rootFrame.Navigated += this.RootFrame_FirstNavigated;
                rootFrame.Navigated += OnNavigated;

                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                string tmpIp = SettingsService.LOCAL_SETTINGS.Values["ip"] as string;
                if (string.IsNullOrEmpty(tmpIp))
                {
                    if (!rootFrame.Navigate(typeof(SettingsPage), e.Arguments))
                    {
                        throw new Exception("Failed to create initial (Settings) page");
                    }
                }
                else if (!rootFrame.Navigate(typeof(MainPage), e.Arguments))
                {
                    throw new Exception("Failed to create initial (MainPage) page");
                }
            }

            // Register a handler for BackRequested events and set the
            // visibility of the Back button
            SystemNavigationManager.GetForCurrentView().BackRequested += OnBackRequested;

            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                rootFrame.CanGoBack ?
                AppViewBackButtonVisibility.Visible :
                AppViewBackButtonVisibility.Collapsed;

            // Ensure the current window is active
            Window.Current.Activate();

            try
            {
                // Install the main VCD. Since there's no simple way to test that the VCD has been imported, or that it's your most recent
                // version, it's not unreasonable to do this upon app load.
                StorageFile vcdStorageFile = await Package.Current.InstalledLocation.GetFileAsync(@"Cortana\HughVoiceCommand.xml");

                await Windows.ApplicationModel.VoiceCommands.VoiceCommandDefinitionManager.InstallCommandDefinitionsFromStorageFileAsync(vcdStorageFile);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Installing Voice Commands Failed: " + ex.ToString());
            }
        }

        /// <summary>
        /// Restores the content transitions after the app has launched.
        /// </summary>
        /// <param name="sender">The object where the handler is attached.</param>
        /// <param name="e">Details about the navigation event.</param>
        private void RootFrame_FirstNavigated(object sender, NavigationEventArgs e)
        {
            var rootFrame = sender as Frame;
            rootFrame.ContentTransitions = this.transitions ?? new TransitionCollection() { new NavigationThemeTransition() };
            rootFrame.Navigated -= this.RootFrame_FirstNavigated;
        }

        private void OnNavigated(object sender, NavigationEventArgs e)
        {
            // Each time a navigation event occurs, update the Back button's visibility
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                ((Frame)sender).CanGoBack ?
                AppViewBackButtonVisibility.Visible :
                AppViewBackButtonVisibility.Collapsed;
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

            // TODO: Save application state and stop any background activity
            deferral.Complete();
        }

        private void OnBackRequested(object sender, BackRequestedEventArgs e)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            if (rootFrame.CanGoBack)
            {
                e.Handled = true;
                rootFrame.GoBack();
            }
        }

        /**
         * The OnActivated replaces OnLaunched when this app is started in an
         * exceptional way (ie a background task or in our case using Cortana)
         */
        protected async override void OnActivated(IActivatedEventArgs e)
        {
            base.OnActivated(e);

            //Type navigationToPageType;
            //HughLib.VoiceCommand? command = null; We don't have such a struct yet, it doesn't seem necessary

            //We'll prep the list of lights/groups here. I'd prefer not to do this and load from internal storage
            //a centralised .xml or something, but that would require effort. If you're doomed to continue with this
            //project, I sincerely hope you'll implement this for me.  
            List<HughLib.Light> Lights = await HueLightService.RetrieveLights();
            List<HughLib.Light> Groups = await HueLightService.RetrieveGroups();

            // This app can be launched directly with the following commands: Turn on ALL lights or turn on LIGHT_NAME
            // As of now those commands start the app to execute the command, but we can also let this get handled by
            // the backgroundservice. Maybe a smart idea
            if (e.Kind == ActivationKind.VoiceCommand)
            {
                // The arguments can represent many different activation types. Cast it so we can get the
                // parameters we care about out.
                var commandArgs = e as VoiceCommandActivatedEventArgs;

                Windows.Media.SpeechRecognition.SpeechRecognitionResult speechRecognitionResult = commandArgs.Result;

                // Get the name of the voice command and the text spoken. See HughVoiceCommand.xml for
                // the <Command> tags this can be filled with.
                string voiceCommandName = speechRecognitionResult.RulePath[0];
                string textSpoken = speechRecognitionResult.Text;
                System.Diagnostics.Debug.WriteLine("Command name: " + voiceCommandName);
                System.Diagnostics.Debug.WriteLine("Command name: " + textSpoken);

                // The commandMode is either "voice" or "text", and it indictes how the voice command
                // was entered by the user.
                // Apps should respect "text" mode by providing feedback in silent form.               
                string commandMode = speechRecognitionResult.SemanticInterpretation.Properties["commandMode"].FirstOrDefault();

                switch (voiceCommandName)
                {
                    case "AllLamps":
                        // Access the value of the {} phrase in the voice command
                        string state = speechRecognitionResult.SemanticInterpretation.Properties["states"].FirstOrDefault();
                        if (state == "On")
                        {
                            System.Diagnostics.Debug.WriteLine("Turn lights on");
                            HughLib.Light dimmer2 = Groups.Find(x => x.name == "Dimmer 2"); //For now let's assume this exists     
                            dimmer2.on = true; //I didn't know I had to do this.                    
                            var response = await HueLightService.GroupOnTask(dimmer2); 
                            System.Diagnostics.Debug.WriteLine("Response: "+response);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("Turn lights off");
                            HughLib.Light dimmer2 = Groups.Find(x => x.name == "Dimmer 2");
                            dimmer2.on = false;
                            var response = await HueLightService.GroupOnTask(dimmer2);
                            System.Diagnostics.Debug.WriteLine("Response: " + response);
                        }
                        break;
                    case "SpecificLamps":
                        throw new NotImplementedException();
                    default:
                        System.Diagnostics.Debug.WriteLine("Unkown command, doing nothing");
                        break;
                }
            } //Started via the background task
            else if (e.Kind == ActivationKind.Protocol)
            {
                // Extract the launch context. In this case, we're just using the destination from the phrase set (passed
                // along in the background task inside Cortana), which makes no attempt to be unique. A unique id or 
                // identifier is ideal for more complex scenarios. We let the destination page check if the 
                // destination trip still exists, and navigate back to the trip list if it doesn't.
                /*var commandArgs = args as ProtocolActivatedEventArgs;
                Windows.Foundation.WwwFormUrlDecoder decoder = new Windows.Foundation.WwwFormUrlDecoder(commandArgs.Uri.Query);
                var destination = decoder.GetFirstValueByName("LaunchContext");

                navigationCommand = new ViewModel.TripVoiceCommand(
                                        "protocolLaunch",
                                        "text",
                                        "destination",
                                        destination);

                navigationToPageType = typeof(View.TripDetails);*/
                throw new NotImplementedException();
            }

            // Re"peat the same basic initialization as OnLaunched() above, taking into account whether
            // or not the app is already active.
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            //If we have any special navigation cases from a command (Show detailspage for a light)
            //We'll want to replace this
            rootFrame.Navigate(typeof(MainPage)); 

            // Ensure the current window is active
            Window.Current.Activate();

            // Register a handler for BackRequested events and set the
            // visibility of the Back button
            SystemNavigationManager.GetForCurrentView().BackRequested += OnBackRequested;

            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility =
                rootFrame.CanGoBack ?
                AppViewBackButtonVisibility.Visible :
                AppViewBackButtonVisibility.Collapsed;

            // Ensure the current window is active
            Window.Current.Activate();
        }
    }
}
