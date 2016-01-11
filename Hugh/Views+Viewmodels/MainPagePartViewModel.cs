using Hugh.Services;
using HughLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Hugh.Views_Viewmodels
{
    public abstract class IRefreshable { protected bool _refreshing; public abstract void RefreshContent(); }

    public class LightsPartViewModel : IRefreshable 
    {
        public ObservableCollection<Light> DisplayLights = new ObservableCollection<Light>();

        public LightsPartViewModel()
        {
            DisplayLights.CollectionChanged += LightsCollectionChanged;
        }

        public override async void RefreshContent()
        {
            if (!this._refreshing)
            {
                System.Diagnostics.Debug.WriteLine("Refreshing lights now");
                this._refreshing = true;
                var retrievedLights = await HueLightService.RetrieveLights();
                if (retrievedLights == null)
                {
                    MainPage.ShowAnotherErrorDialogue();
                }
                else
                {
                    retrievedLights.OrderBy(x => x.id);
                    //Do not override observable collection, messes up the binding
                    DisplayLights.Clear();
                    retrievedLights.ForEach(x => DisplayLights.Add(x));           
                }
                this._refreshing = false;
            }
        }

        public async void ItemView_ItemClick(object sender, ItemClickEventArgs e)
        {
            List<Light> retrievedLights = await HueLightService.RetrieveLights();
            if (retrievedLights != null)
            {
                retrievedLights.OrderBy(x => x.id);
                DisplayLights.Clear();
                retrievedLights.ForEach(x => DisplayLights.Add(x));
                Light selectedLight = e.ClickedItem as Light;
                if (selectedLight != null)
                {
                    Frame rootFrame = Window.Current.Content as Frame;
                    if (!rootFrame.Navigate(typeof(DetailPage), selectedLight))
                        System.Diagnostics.Debug.WriteLine("NavigationFailedExceptionMessage");
                }
            }
        }

        public void LightsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (Light l in e.OldItems)
                {
                    //Removed items
                    l.PropertyChanged -= LightPropertyChanged;
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (Light l in e.NewItems)
                {
                    //Added items
                    l.PropertyChanged += LightPropertyChanged;
                }
            }
        }

        public void LightPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //XAML handles the rest, no codebehind is further neccesary. Only the existence of this.
        }
    }

    public class GroupsPartViewModel : IRefreshable
    {
        public ObservableCollection<Light> DisplayGroups = new ObservableCollection<Light>();

        public GroupsPartViewModel()
        {
            DisplayGroups.CollectionChanged += GroupsCollectionChanged;
        }

        public override async void RefreshContent()
        {
            if (!this._refreshing)
            {
                System.Diagnostics.Debug.WriteLine("Refreshing groups now");
                this._refreshing = true;
                var retrieveGroups = await HueLightService.RetrieveGroups();
                if (retrieveGroups == null)
                {
                    MainPage.ShowAnotherErrorDialogue();
                }
                else
                {
                    retrieveGroups.OrderBy(x => x.id);
                    //Do not override observable collection, messes up the binding
                    DisplayGroups.Clear();
                    retrieveGroups.ForEach(x => DisplayGroups.Add(x));
                }
                this._refreshing = false;
            }
        }

        public async void ItemView_ItemClick(object sender, ItemClickEventArgs e)
        {
            List<Light> retrievedGroups = await HueLightService.RetrieveGroups();
            if (retrievedGroups != null)
            {
                retrievedGroups.OrderBy(x => x.id);
                DisplayGroups.Clear();
                retrievedGroups.ForEach(x => DisplayGroups.Add(x));
                Light selectedLight = e.ClickedItem as Light;
                if (selectedLight != null)
                {
                    Frame rootFrame = Window.Current.Content as Frame;
                    if (!rootFrame.Navigate(typeof(DetailPage), selectedLight))
                        System.Diagnostics.Debug.WriteLine("NavigationFailedExceptionMessage");
                }
            }
        }

        public void GroupsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (Light l in e.OldItems)
                {
                    //Removed items
                    l.PropertyChanged -= GroupPropertyChanged;
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (Light l in e.NewItems)
                {
                    //Added items
                    l.PropertyChanged += GroupPropertyChanged;
                }
            }
        }

        public void GroupPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //XAML handles the rest, no codebehind is further neccesary. Only the existence of this.
        }
    }
}

