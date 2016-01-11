using Hugh.Common;
using Hugh.Services;
using HughLib;
using System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;


// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace Hugh.Views_Viewmodels
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DetailPage : Page
    {
        private readonly NavigationHelper _navigationHelper;

        private bool _pageLoaded;
        private Light _light;

        public DetailPage()
        {
            this.InitializeComponent();

            this._navigationHelper = new NavigationHelper(this);
            this._navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this._navigationHelper.SaveState += this.NavigationHelper_SaveState;
        }



        /// <summary>
        /// Gets the <see cref="NavigationHelper"/> associated with this <see cref="Page"/>.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this._navigationHelper; }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            this._pageLoaded = true;
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
        /// session.  The state will be null the first time a page is visited.</param>
        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            this._pageLoaded = false;
            this._light = e.NavigationParameter as Light;
            this.DataContext = this._light;
            if(_light.isGroup)
            {
                toggleOn.Header = "Group state: ";
            }
            else
            {
                toggleOn.Header = "Light state: ";
            }
        }

        /// <summary>
        /// Preserves state associated with this page in case the application is suspended or the
        /// page is discarded from the navigation cache.  Values must conform to the serialization
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
        /// The methods provided in this section are simply used to allow
        /// NavigationHelper to respond to the page's navigation methods.
        /// <para>
        /// Page specific logic should be placed in event handlers for the  
        /// <see cref="NavigationHelper.LoadState"/>
        /// and <see cref="NavigationHelper.SaveState"/>.
        /// The navigation parameter is available in the LoadState method 
        /// in addition to page state preserved during an earlier session.
        /// </para>
        /// </summary>
        /// <param name="e">Provides data for navigation methods and event
        /// handlers that cannot cancel the navigation request.</param>
        protected override void OnNavigatedTo(NavigationEventArgs inav)
        {
            Frame rootFrame = Window.Current.Content as Frame;

            // SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;

            //SystemNavigationManager.GetForCurrentView().BackRequested += (s, e) =>
            //{
            //   GoBack();
            //};

            this._navigationHelper.OnNavigatedTo(inav);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this._navigationHelper.OnNavigatedFrom(e);
        }

        private async void btnSetName_Click(object sender, RoutedEventArgs e)
        {
            if (this._pageLoaded)
            {
                this._light.name = txtName.Text;
                var response = (_light.isGroup) ? await HueLightService.GroupNameTask(_light) : await HueLightService.LightNameTask(_light);
                if (string.IsNullOrEmpty(response))
                    await new MessageDialog("Error while setting light properties. Please check your application settings.").ShowAsync();
            }
        }

        private void toggleSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (this._pageLoaded)
            {
                if (sender == toggleOn)
                    LightOn();
                else if (sender == toggleColorloop)
                    LightLoop();
            }
        }

        private void slider_PointerCaptureLost(object sender, PointerRoutedEventArgs e)
        {
            if (this._pageLoaded)
                LightColor();
        }

        private async void LightOn()
        {
            var response = (_light.isGroup)? await HueLightService.GroupOnTask(_light) : await HueLightService.LightOnTask(_light);
            if (string.IsNullOrEmpty(response))
                await new MessageDialog("Error while setting light properties. Please check your application settings.").ShowAsync();
        }

        private async void LightLoop()
        {
            var response = (_light.isGroup) ? await HueLightService.GroupLoopTask(_light) : await HueLightService.LightLoopTask(_light);
            if (string.IsNullOrEmpty(response))
                await new MessageDialog("Error while setting light properties. Please check your application settings.").ShowAsync();
        }

        private async void LightColor()
        {
            var response = (_light.isGroup) ? await HueLightService.GroupColorTask(_light) : await HueLightService.LightColorTask(_light);
            if (string.IsNullOrEmpty(response))
                await new MessageDialog("Error while setting light properties. Please check your application settings.").ShowAsync();
        }

        void GoBack()
        {
            if (this.Frame != null && this.Frame.CanGoBack) this.Frame.GoBack();
        }

    }
}
