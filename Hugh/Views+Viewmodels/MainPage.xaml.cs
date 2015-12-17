using System;
using Windows.Foundation;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;
using Hugh.Common;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace Hugh.Views_Viewmodels
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private readonly NavigationHelper navigationHelper;

        public bool _initiallyLoaded;
        private static bool _isShowingDialog;
        private static MessageDialog messageDialog = new MessageDialog("Please press the link button.");

        //Partviewmodels for the pivots
        public LightsPartViewModel LightsPartViewModel = new LightsPartViewModel();
        public GroupsPartViewModel GroupsPartViewModel = new GroupsPartViewModel();

        public MainPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Required;

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

            System.Diagnostics.Debug.WriteLine("Hello! I am a MainPage instance!");
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
            Refresh();
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


        //Following methods are for the command bar, shared between pivot items.
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!Frame.Navigate(typeof(SettingsPage)))
                System.Diagnostics.Debug.WriteLine("NavigationFailedExceptionMessage");
        }

        public static void ShowErrorDialogue()
        {
            if (!_isShowingDialog)
            {
                //Asyncoperation unused, because the control lies solely with the user.
                IAsyncOperation<IUICommand> showingDialogue = messageDialog.ShowAsync();            
                _isShowingDialog = true;
            }
        }

        public static async void ShowAnotherErrorDialogue()
        {
            if (!_isShowingDialog)
            {
                //Asyncoperation unused, because the control lies solely with the user.
                await new MessageDialog("Error while retrieving lights. Please check your application settings.").ShowAsync();
                _isShowingDialog = true;
            }
            
        }

        private void Refresh()
        {
            //For now we'll refresh both. I would like to know how to refresh only the current active pivotitem
            LightsPartViewModel.RefreshContent();
            //GroupsPartViewModel.RefreshContent();
        }

        private void RefreshLightsButton_Click(object sender, RoutedEventArgs e)
        {
            Refresh();
        }
    }
}
