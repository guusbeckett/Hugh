using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.SpeechRecognition;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;

// The Blank Application template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace HueRemote
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
            var storageFile = await Windows.Storage.StorageFile.GetFileFromApplicationUriAsync(new Uri("ms-appx:///VoiceCommandDefinition.xml"));

            await Windows.ApplicationModel.VoiceCommands.VoiceCommandDefinitionManager.InstallCommandDefinitionsFromStorageFileAsync(storageFile);

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
                if (!rootFrame.Navigate(typeof(MainPage), e.Arguments))
                {
                    throw new Exception("Failed to create initial page");
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

        protected override async void OnActivated(IActivatedEventArgs args)
        {
            base.OnActivated(args);

            if (args.Kind == ActivationKind.VoiceCommand)
            {
                var voiceCommandArgs = args as VoiceCommandActivatedEventArgs;

                if (voiceCommandArgs != null)
                {
                    SpeechRecognitionResult result = voiceCommandArgs.Result;

                    string commandName = result.RulePath.FirstOrDefault();
                    string text = result.Text;

                    switch (commandName)
                    {
                        case "setLampState":
                            if(text.Contains("on"))
                            {
                                await LightsGroupOnTask(true);
                            }
                            else if(text.Contains("off"))
                            {
                                await LightsGroupOnTask(false);
                            }
                            break;
                        case "setLampColor":
                            await LightsGroupColorTask(getColorFromText(text));
                            break;
                    }
                }
            }
        }


        private async Task<string> LightsGroupOnTask(bool lightsOn)
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(10000);

            try
            {
                HttpClient client = new HttpClient();
                HttpStringContent content = new HttpStringContent(string.Format("{{ \"on\": {0} }}", lightsOn.ToString().ToLower()), Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json");
                string ip, username;
                int port;
                MainPage.RetrieveSettings(out ip, out port, out username);
                var response = await client.PutAsync(new Uri(string.Format("http://{0}:{1}/api/{2}/groups/{3}/action", ip, port, username, 2)), content).AsTask(cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    return string.Empty;
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine(jsonResponse);

                return jsonResponse;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                return string.Empty;
            }
        }

        private async Task<string> LightsGroupColorTask(Light.PredefinedColor lightsColor)
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(10000);
            int hue = 0;
            int sat = 0;
            switch(lightsColor)
            {
                case Light.PredefinedColor.BLUE:
                    sat = 254;
                    hue = 46920;
                    break;
                case Light.PredefinedColor.GREEN:
                    sat = 254;
                    hue = 25500;
                    break;
                case Light.PredefinedColor.ORANGE:
                    sat = 254;
                    hue = 9000;
                    break;
                case Light.PredefinedColor.PINK:
                    sat = 254;
                    hue = 56100;
                    break;
                case Light.PredefinedColor.RED:
                    sat = 254;
                    hue = 0;
                    break;
                case Light.PredefinedColor.YELLOW:
                    sat = 254;
                    hue = 18000;
                    break;
                case Light.PredefinedColor.WHITE:
                    sat = 0;
                    break;
            }

            try
            {
                HttpClient client = new HttpClient();
                HttpStringContent content = new HttpStringContent(string.Format("{{ \"hue\": {0}, \"sat\": {1}, \"bri\": {2} }}", hue, sat, 254), Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json");
                string ip, username;
                int port;
                MainPage.RetrieveSettings(out ip, out port, out username);
                var response = await client.PutAsync(new Uri(string.Format("http://{0}:{1}/api/{2}/groups/{3}/action", ip, port, username, 2)), content).AsTask(cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    return string.Empty;
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine(jsonResponse);

                return jsonResponse;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                return string.Empty;
            }
        }

        private Light.PredefinedColor getColorFromText(string text)
        {
            if (text.Contains("red"))
            {
                return Light.PredefinedColor.RED;
            }
            if (text.Contains("green"))
            {
                return Light.PredefinedColor.GREEN;
            }
            if (text.Contains("blue"))
            {
                return Light.PredefinedColor.BLUE;
            }
            if (text.Contains("yellow"))
            {
                return Light.PredefinedColor.YELLOW;
            }
            if (text.Contains("orange"))
            {
                return Light.PredefinedColor.ORANGE;
            }
            if (text.Contains("pink"))
            {
                return Light.PredefinedColor.PINK;
            }
            if (text.Contains("white"))
            {
                return Light.PredefinedColor.WHITE;
            }
            return Light.PredefinedColor.WHITE;
        }

    }
}