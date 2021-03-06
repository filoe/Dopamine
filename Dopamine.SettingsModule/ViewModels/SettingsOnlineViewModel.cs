﻿using Digimezzo.Utilities.IO;
using Digimezzo.Utilities.Settings;
using Digimezzo.Utilities.Utils;
using Dopamine.Common.Services.Dialog;
using Dopamine.Common.Services.Provider;
using Dopamine.Common.Services.Scrobbling;
using Dopamine.Common.Base;
using Dopamine.Common.Prism;
using Microsoft.Practices.Unity;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using System;
using Digimezzo.Utilities.Log;
using Digimezzo.Utilities.Helpers;
using System.Linq;

namespace Dopamine.SettingsModule.ViewModels
{
    public class SettingsOnlineViewModel : BindableBase
    {
        #region Private
        private IUnityContainer container;
        private IProviderService providerService;
        private IDialogService dialogService;
        private IEventAggregator eventAggregator;
        private ObservableCollection<SearchProvider> searchProviders;
        private SearchProvider selectedSearchProvider;
        private IScrobblingService scrobblingService;
        private bool isLastFmSignInInProgress;
        private bool checkBoxDownloadArtistInformationChecked;
        private bool checkBoxDownloadLyricsChecked;
        private bool checkBoxChartLyricsChecked;
        private bool checkBoxLoloLyricsChecked;
        private bool checkBoxLyricWikiaChecked;
        private bool checkBoxMetroLyricsChecked;
        private ObservableCollection<NameValue> timeouts;
        private NameValue selectedTimeout;
        #endregion

        #region Commands
        public DelegateCommand AddCommand { get; set; }
        public DelegateCommand EditCommand { get; set; }
        public DelegateCommand RemoveCommand { get; set; }
        public DelegateCommand LastfmSignInCommand { get; set; }
        public DelegateCommand LastfmSignOutCommand { get; set; }
        public DelegateCommand CreateLastFmAccountCommand { get; set; }
        #endregion

        #region Properties
        public ObservableCollection<NameValue> Timeouts
        {
            get { return this.timeouts; }
            set { SetProperty<ObservableCollection<NameValue>>(ref this.timeouts, value); }
        }

        public NameValue SelectedTimeout
        {
            get { return this.selectedTimeout; }
            set
            {
                SettingsClient.Set<int>("Lyrics", "TimeoutSeconds", value.Value);
                SetProperty<NameValue>(ref this.selectedTimeout, value);
            }
        }

        public bool CheckBoxChartLyricsChecked
        {
            get { return this.checkBoxChartLyricsChecked; }
            set
            {
                this.AddRemoveLyricsDownloadProvider("chartlyrics", value);
                SetProperty<bool>(ref this.checkBoxChartLyricsChecked, value);
            }
        }

        public bool CheckBoxLoloLyricsChecked
        {
            get { return this.checkBoxLoloLyricsChecked; }
            set
            {
                this.AddRemoveLyricsDownloadProvider("lololyrics", value);
                SetProperty<bool>(ref this.checkBoxLoloLyricsChecked, value);
            }
        }

        public bool CheckBoxLyricWikiaChecked
        {
            get { return this.checkBoxLyricWikiaChecked; }
            set
            {
                this.AddRemoveLyricsDownloadProvider("lyricwikia", value);
                SetProperty<bool>(ref this.checkBoxLyricWikiaChecked, value);
            }
        }

        public bool CheckBoxMetroLyricsChecked
        {
            get { return this.checkBoxMetroLyricsChecked; }
            set
            {
                this.AddRemoveLyricsDownloadProvider("metrolyrics", value);
                SetProperty<bool>(ref this.checkBoxMetroLyricsChecked, value);
            }
        }

        public ObservableCollection<SearchProvider> SearchProviders
        {
            get { return this.searchProviders; }
            set
            {
                SetProperty<ObservableCollection<SearchProvider>>(ref this.searchProviders, value);
                this.SelectedSearchProvider = null;
            }
        }

        public SearchProvider SelectedSearchProvider
        {
            get { return this.selectedSearchProvider; }
            set
            {
                SetProperty<SearchProvider>(ref this.selectedSearchProvider, value);
                this.EditCommand.RaiseCanExecuteChanged();
                this.RemoveCommand.RaiseCanExecuteChanged();
            }
        }

        public bool CheckBoxDownloadArtistInformationChecked
        {
            get { return this.checkBoxDownloadArtistInformationChecked; }
            set
            {
                SettingsClient.Set<bool>("Lastfm", "DownloadArtistInformation", value);
                SetProperty<bool>(ref this.checkBoxDownloadArtistInformationChecked, value);
            }
        }

        public bool CheckBoxDownloadLyricsChecked
        {
            get { return this.checkBoxDownloadLyricsChecked; }
            set
            {
                SettingsClient.Set<bool>("Lyrics", "DownloadLyrics", value);
                SetProperty<bool>(ref this.checkBoxDownloadLyricsChecked, value);
                this.eventAggregator.GetEvent<SettingDownloadLyricsChanged>().Publish(value);
            }
        }

        public bool IsLastFmSignedIn
        {
            get { return this.scrobblingService.SignInState == SignInState.SignedIn; }
        }

        public string LastFmUsername
        {
            get { return this.scrobblingService.Username; }
            set
            {
                this.scrobblingService.Username = value;
            }
        }

        public bool IsLastFmSigningIn
        {
            get { return this.isLastFmSignInInProgress; }
            set
            {
                SetProperty<bool>(ref this.isLastFmSignInInProgress, value);
            }
        }

        public bool IsLastFmSignInError
        {
            get { return this.scrobblingService.SignInState == SignInState.Error; }
        }
        #endregion

        #region Construction
        public SettingsOnlineViewModel(IUnityContainer container, IProviderService providerService, IDialogService dialogService, IScrobblingService scrobblingService, IEventAggregator eventAggregator)
        {
            this.container = container;
            this.providerService = providerService;
            this.dialogService = dialogService;
            this.scrobblingService = scrobblingService;
            this.eventAggregator = eventAggregator;

            this.scrobblingService.SignInStateChanged += (_) =>
            {
                this.IsLastFmSigningIn = false;
                OnPropertyChanged(() => this.IsLastFmSignedIn);
                OnPropertyChanged(() => this.LastFmUsername);
                OnPropertyChanged(() => this.IsLastFmSignInError);
            };

            this.AddCommand = new DelegateCommand(() => this.AddSearchProvider());
            this.EditCommand = new DelegateCommand(() => { this.EditSearchProvider(); }, () => { return this.SelectedSearchProvider != null; });
            this.RemoveCommand = new DelegateCommand(() => { this.RemoveSearchProvider(); }, () => { return this.SelectedSearchProvider != null; });
            this.LastfmSignInCommand = new DelegateCommand(async () =>
            {
                this.IsLastFmSigningIn = true;
                await this.scrobblingService.SignIn();

            });
            this.LastfmSignOutCommand = new DelegateCommand(() => this.scrobblingService.SignOut());
            this.CreateLastFmAccountCommand = new DelegateCommand(() => Actions.TryOpenLink(Constants.LastFmJoinLink));

            this.GetSearchProvidersAsync();

            this.providerService.SearchProvidersChanged += (_, __) => this.GetSearchProvidersAsync();

            this.GetCheckBoxesAsync();
            this.GetTimeoutsAsync();
        }
        #endregion

        #region Private
        private async void GetSearchProvidersAsync()
        {
            var providersList = await this.providerService.GetSearchProvidersAsync();
            var localProviders = new ObservableCollection<SearchProvider>();

            foreach (SearchProvider provider in providersList)
            {
                localProviders.Add(provider);
            }

            this.SearchProviders = localProviders;
        }

        private void AddSearchProvider()
        {
            SettingsOnlineAddEditSearchProvider view = this.container.Resolve<SettingsOnlineAddEditSearchProvider>();
            view.DataContext = this.container.Resolve<SettingsOnlineAddEditSearchProviderViewModel>(new DependencyOverride(typeof(SearchProvider), new SearchProvider()));

            string dialogTitle = ResourceUtils.GetStringResource("Language_Add");

            this.dialogService.ShowCustomDialog(
                0xe104,
                14,
                dialogTitle,
                view,
                450,
                0,
                false,
                true,
                true,
                true,
                ResourceUtils.GetStringResource("Language_Ok"),
                ResourceUtils.GetStringResource("Language_Cancel"),
                ((SettingsOnlineAddEditSearchProviderViewModel)view.DataContext).AddSearchProviderAsync);
        }

        private void EditSearchProvider()
        {
            SettingsOnlineAddEditSearchProvider view = this.container.Resolve<SettingsOnlineAddEditSearchProvider>();
            view.DataContext = this.container.Resolve<SettingsOnlineAddEditSearchProviderViewModel>(new DependencyOverride(typeof(SearchProvider), this.selectedSearchProvider));

            string dialogTitle = ResourceUtils.GetStringResource("Language_Edit");

            this.dialogService.ShowCustomDialog(
                0xe104,
                14,
                dialogTitle,
                view,
                450,
                0,
                false,
                true,
                true,
                true,
                ResourceUtils.GetStringResource("Language_Ok"),
                ResourceUtils.GetStringResource("Language_Cancel"),
                ((SettingsOnlineAddEditSearchProviderViewModel)view.DataContext).UpdateSearchProviderAsync);
        }

        private void RemoveSearchProvider()
        {
            if (this.dialogService.ShowConfirmation(0xe11b, 16, ResourceUtils.GetStringResource("Language_Remove"), ResourceUtils.GetStringResource("Language_Confirm_Remove_Online_Search_Provider").Replace("%provider%", this.selectedSearchProvider.Name), ResourceUtils.GetStringResource("Language_Yes"), ResourceUtils.GetStringResource("Language_No")))
            {
                var isRemoveSuccess = this.providerService.RemoveSearchProvider(this.selectedSearchProvider);

                if (!isRemoveSuccess)
                {
                    this.dialogService.ShowNotification(
                        0xe711,
                        16,
                        ResourceUtils.GetStringResource("Language_Error"),
                        ResourceUtils.GetStringResource("Language_Error_Removing_Online_Search_Provider").Replace("%provider%", this.selectedSearchProvider.Name),
                        ResourceUtils.GetStringResource("Language_Ok"),
                        true,
                        ResourceUtils.GetStringResource("Language_Log_File"));
                }
            }
        }

        private async void GetCheckBoxesAsync()
        {
            await Task.Run(() =>
            {
                this.CheckBoxDownloadArtistInformationChecked = SettingsClient.Get<bool>("Lastfm", "DownloadArtistInformation");
                this.CheckBoxDownloadLyricsChecked = SettingsClient.Get<bool>("Lyrics", "DownloadLyrics");

                string lyricsProviders = SettingsClient.Get<string>("Lyrics", "Providers");

                // Set the backing field to avoid saving into the settings
                this.checkBoxChartLyricsChecked = lyricsProviders.ToLower().Contains("chartlyrics");
                this.checkBoxLoloLyricsChecked = lyricsProviders.ToLower().Contains("lololyrics");
                this.checkBoxLyricWikiaChecked = lyricsProviders.ToLower().Contains("lyricwikia");
                this.checkBoxMetroLyricsChecked = lyricsProviders.ToLower().Contains("metrolyrics");

                OnPropertyChanged(() => this.CheckBoxChartLyricsChecked);
                OnPropertyChanged(() => this.CheckBoxLoloLyricsChecked);
                OnPropertyChanged(() => this.CheckBoxLyricWikiaChecked);
                OnPropertyChanged(() => this.CheckBoxMetroLyricsChecked);
            });
        }

        private void AddRemoveLyricsDownloadProvider(string provider, bool add)
        {
            try
            {
                string lyricsProviders = SettingsClient.Get<string>("Lyrics", "Providers");
                var lyricsProvidersList = new List<string>(lyricsProviders.ToLower().Split(';'));

                if (add)
                {
                    if (!lyricsProvidersList.Contains(provider)) lyricsProvidersList.Add(provider);
                }
                else
                {
                    if (lyricsProvidersList.Contains(provider)) lyricsProvidersList.Remove(provider);
                }

                string[] arr = lyricsProvidersList.ToArray();
                SettingsClient.Set<string>("Lyrics", "Providers", string.Join(";", arr));
            }
            catch (Exception ex)
            {
                LogClient.Error("Could not add/remove lyrics download providers. Add = '{0}'. Exception: {1}", add.ToString(), ex.Message);
            }
        }

        private async void GetTimeoutsAsync()
        {
            var localTimeouts = new ObservableCollection<NameValue>();

            await Task.Run(() =>
            {
                localTimeouts.Add(new NameValue { Name = "None", Value = 0 });
                localTimeouts.Add(new NameValue { Name = "1", Value = 1 });
                localTimeouts.Add(new NameValue { Name = "2", Value = 2 });
                localTimeouts.Add(new NameValue { Name = "5", Value = 5 });
                localTimeouts.Add(new NameValue { Name = "10", Value = 10 });
                localTimeouts.Add(new NameValue { Name = "20", Value = 20 });
                localTimeouts.Add(new NameValue { Name = "30", Value = 30 });
                localTimeouts.Add(new NameValue { Name = "40", Value = 40 });
                localTimeouts.Add(new NameValue { Name = "50", Value = 50 });
                localTimeouts.Add(new NameValue { Name = "60", Value = 60 });
            });

            this.Timeouts = localTimeouts;

            NameValue localSelectedTimeout = null;
            await Task.Run(() => localSelectedTimeout = this.Timeouts.Where((svp) => svp.Value == SettingsClient.Get<int>("Lyrics", "TimeoutSeconds")).Select((svp) => svp).First());
            this.SelectedTimeout = localSelectedTimeout;
        }
        #endregion
    }
}
