using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.Storage;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.Web.Http;
using Windows.ApplicationModel;
using Hugh.Common;
using HughLib;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Hugh
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public static ApplicationData APP_DATA = ApplicationData.Current;
        public static ApplicationDataContainer LOCAL_SETTINGS = APP_DATA.LocalSettings;

        private readonly NavigationHelper navigationHelper;

        private ObservableCollection<Light> _lights = new ObservableCollection<Light>();
        private bool _refreshing;
        public bool _initiallyLoaded;
        private bool _isShowingDialog;
        private static MessageDialog messageDialog = new MessageDialog("Please press the link button.");

        public MainPage()
        {
            this.InitializeComponent();

            this.NavigationCacheMode = NavigationCacheMode.Required;

            this._lights = new ObservableCollection<Light>();
            lvLights.ItemsSource = this._lights;

            this.NavigationCacheMode = NavigationCacheMode.Required;

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;
        }

        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Populates the page with content passed during navigation. Any saved state is also
        /// provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="sender">
        /// The source of the event; typically <see cref="NavigationHelper"/>.
        /// </param>
        /// <param name="e">Event data that provides both the navigation parameter passed to
        /// <see cref="Frame.Navigate(Type, Object)"/> when this page was initially requested and
        /// a dictionary of state preserved by this page during an earlier
        /// session. The state will be null the first time a page is visited.</param>
        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            Frame.BackStack.Clear();
            RefreshLights();
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache. Values must conform to the serialization
        /// requirements of <see cref="SuspensionManager.SessionState"/>.
        /// </summary>
        /// <param name="sender">The source of the event; typically <see cref="NavigationHelper"/>.</param>
        /// <param name="e">Event data that provides an empty dictionary to be populated with
        /// serializable state.</param>
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            // TODO: Save the unique state of the page here.
        }

        /// <summary>
        /// Invoked when this page is about to be displayed in a Frame.
        /// </summary>
        /// <param name="e">Event data that describes how this page was reached.
        /// This parameter is typically used to configure the page.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        private void ItemView_ItemClick(object sender, ItemClickEventArgs e)
        {
            Light selectedLight = e.ClickedItem as Light;
            if (selectedLight != null)
            {
                Frame rootFrame = Window.Current.Content as Frame;
                if (!rootFrame.Navigate(typeof(DetailPage), selectedLight))
                    System.Diagnostics.Debug.WriteLine("NavigationFailedExceptionMessage");
            }
        }

        private void RefreshLightsButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshLights();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Frame.Navigate(typeof(SettingsPage)))
                System.Diagnostics.Debug.WriteLine("NavigationFailedExceptionMessage");
        }

        private async void RefreshLights()
        {
            if (!this._refreshing)
            {
                System.Diagnostics.Debug.WriteLine("Refreshing lights now");
                this._refreshing = true;
                if (!_initiallyLoaded)
                {

                    string tmpUsername = MainPage.LOCAL_SETTINGS.Values["user"] as string;

                    if (string.IsNullOrEmpty(tmpUsername))
                    {
                        var responseBridgeUsername = await RetrieveUsername();
                        MainPage.LOCAL_SETTINGS.Values["user"] = responseBridgeUsername;
                    }
                    _initiallyLoaded = true;
                }


                var response = await RetrieveLights();
                if (!string.IsNullOrEmpty(response))
                {
                    ParseJson(response);
                    if (this._lights != null)
                        lvLights.ItemsSource = this._lights;
                }
                else
                    await new MessageDialog("Error while retrieving lights. Please check your application settings.").ShowAsync();

                this._refreshing = false;
            }
        }

        private async Task<string> RetrieveLights()
        {
            var cts = new CancellationTokenSource();
            cts.CancelAfter(5000);

            try
            {
                HttpClient client = new HttpClient();
                string ip, username;
                int port;
                MainPage.RetrieveSettings(out ip, out port, out username);
                var response = await client.GetAsync(new Uri(string.Format("http://{0}:{1}/api/{2}/lights/", ip, port, username))).AsTask(cts.Token);

                if (!response.IsSuccessStatusCode)
                {
                    return string.Empty;
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();

                System.Diagnostics.Debug.WriteLine(jsonResponse);

                return jsonResponse;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private void ParseJson(string json)
        {
            List<Light> lights = new List<Light>();

            JsonObject jsonObject = JsonObject.Parse(json);
            foreach (string key in jsonObject.Keys)
            {
                string lightId = key;

                if (lightId.Equals("error"))
                {
                    System.Diagnostics.Debug.WriteLine(jsonObject[key]);
                    continue;
                }

                JsonObject lightToAdd;
                Light l = null;

                try
                {
                    lightToAdd = jsonObject.GetNamedObject(key, null);

                    JsonObject lightState = lightToAdd.GetNamedObject("state", null);
                    if (lightState != null)
                    {
                        Light.Effect effect = Light.Effect.EFFECT_NONE;
                        if (lightState.GetNamedString("effect", string.Empty).Equals("colorloop"))
                            effect = Light.Effect.EFFECT_COLORLOOP;

                        l = new Light(Convert.ToInt32(lightId), lightToAdd.GetNamedString("name", string.Empty), lightState.GetNamedBoolean("on", false),
                                        Convert.ToInt32(lightState.GetNamedNumber("hue", 0)), Convert.ToInt32(lightState.GetNamedNumber("sat", 255)),
                                        Convert.ToInt32(lightState.GetNamedNumber("bri", 255)), effect, lightState.GetNamedBoolean("reachable", false));
                    }
                    else
                        l = new Light(Convert.ToInt32(lightId), string.Format("Hue lamp {0}", lightId), true, 20000, 255, 255, Light.Effect.EFFECT_NONE, true);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(string.Format("{0} - {1}", e.Message, e.StackTrace));
                    continue;
                }

                if (l != null)
                    lights.Add(l);
            }

            this._lights = new ObservableCollection<Light>(lights.OrderBy(x => x.id));
        }


        private async Task<string> RetrieveUsername()
        {
            Boolean hasGotUsername = false;
            string jsonResponse = "";
            string usernameRetrieved = "";
            while (!hasGotUsername)
            {
                try
                {
                    HttpClient client = new HttpClient();
                    HttpStringContent content = new HttpStringContent("{\"devicetype\":\"HueApp#ComfyCrew\"}", Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json");
                    string ip, username;
                    int port;
                    MainPage.RetrieveSettings(out ip, out port, out username);
                    var response = await client.PostAsync(new Uri(string.Format("http://{0}:{1}/api/", ip, port)), content);

                    if (!response.IsSuccessStatusCode)
                    {
                        return string.Empty;
                    }

                    jsonResponse = await response.Content.ReadAsStringAsync();

                    System.Diagnostics.Debug.WriteLine(jsonResponse);

                    JsonArray jsonArray = JsonArray.Parse(jsonResponse);
                    ICollection<string> keys = jsonArray.First().GetObject().Keys;
                    if (keys.Contains("error"))
                    {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                        if (!_isShowingDialog)
                        {
                            MainPage.messageDialog.ShowAsync();
                            _isShowingDialog = true;
                        }
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                        await Task.Delay(TimeSpan.FromSeconds(1));
                    }
                    else
                    {
                        hasGotUsername = true;
                        JsonObject succesObject = jsonArray.First().GetObject();

                        System.Diagnostics.Debug.WriteLine(succesObject.Values.First().GetObject().Values.First().GetString());
                        usernameRetrieved = succesObject.Values.First().GetObject().Values.First().GetString();
                    }

                }
                catch (Exception)
                {
                    return string.Empty;
                }
            }
            return usernameRetrieved;
        }

        public static void RetrieveSettings(out string ip, out int port, out string username)
        {
            string tmpIp = MainPage.LOCAL_SETTINGS.Values["ip"] as string;
            int tmpPort = Convert.ToInt32(MainPage.LOCAL_SETTINGS.Values["port"]);
            string tmpUsername = MainPage.LOCAL_SETTINGS.Values["user"] as string;

            if (string.IsNullOrEmpty(tmpIp))
            {
                tmpIp = "";
            }
            if (tmpPort == 0)
            {
                tmpPort = 8000;
            }
            if (string.IsNullOrEmpty(tmpUsername))
            {
                tmpUsername = "";
            }
            ip = tmpIp;
            port = tmpPort;
            username = tmpUsername;
        }

        public static void SetSettings(string ip, int port, string username)
        {
            MainPage.LOCAL_SETTINGS.Values["ip"] = ip;
            MainPage.LOCAL_SETTINGS.Values["port"] = port;
            MainPage.LOCAL_SETTINGS.Values["user"] = username;
        }

    }
}
