﻿
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using CommunityToolkit.Mvvm.Input;
using Dots.Models;
using Dots.Services;
using System.Reactive;
using Dots.Data;
using Dots.Helpers;
using ObservableView;
using ObservableView.Searching.Operators;

namespace Dots.ViewModels
{
    public partial class MainViewModel : ObservableRecipient
    {
        public MainViewModel(DotnetService dotnet, ErrorPopupHelper errorHelper)
        {
            _dotnet = dotnet;
            _errorHelper = errorHelper;
        }

        string _query = "";

        DotnetService _dotnet;
        ErrorPopupHelper _errorHelper;
        List<Sdk> _baseSdks;

        [ObservableProperty]
        bool _selectionEnabled;

        [ObservableProperty]
        bool _isBusy;

        [ObservableProperty]
        Sdk _selectedSdk;

        [ObservableProperty]
        ObservableView<Sdk> _sdks;

        [ObservableProperty]
        string _lastUpdated;

        [ObservableProperty]
        bool _showOnline = true;
        
        [ObservableProperty]
        bool _showInstalled = true;

        [RelayCommand]
        async Task DownloadScript()
        {
            try
            {
                using var client = new HttpClient();
                var response = await client.GetAsync(Constants.InstallerScript);
                var content = await response.Content.ReadAsStringAsync();
                //save file to disk
                var folder = FileSystem.Current.AppDataDirectory;
                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }


                var filename = Path.Combine(folder, "dotnet-install.ps1");
                await File.WriteAllTextAsync(filename, content);
                Debug.WriteLine("done - " + filename);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        [RelayCommand]
        async Task ListRuntimes()
        {
            LastUpdated = " " + DateTime.Now.ToString("MMMM dd, yyyy HH:mm");
            await CheckSdks(true);
        }

        [RelayCommand]
        void FilterSdks(string query)
        {
            _query = query;
            Sdks.Search(_query);
            /*
            var filteredCollection = _baseSdks.Where(s =>
            s.Data.Sdk.Version.ToLowerInvariant().Contains(query.ToLowerInvariant()) ||
            s.Path.ToLowerInvariant().Contains(query.ToLowerInvariant())).ToList();

            foreach (var s in _baseSdks)
            {
                if (!filteredCollection.Contains(s))
                {
                    Sdks.Remove(s);
                }
                else if (!Sdks.Contains(s))
                {
                    Sdks.Add(s);
                }
            }
            */
        }

        [RelayCommand]
        void ToggleSelection()
        {
            SelectionEnabled = !SelectionEnabled;
        }

        [RelayCommand]
        async Task OpenOrDownload(Sdk sdk)
        {
            try
            {
                sdk.IsDownloading = true;
                if (sdk.Installed)
                {
                    await _dotnet.OpenFolder(sdk);
                }
                else
                {
                    await _dotnet.Download(sdk);
                }
                sdk.IsDownloading = false;

            }
            catch (Exception ex)
            {
                sdk.IsDownloading = false;
                await _errorHelper.ShowPopup(ex);
            }
    }

    [RelayCommand]
        async Task InstallOrUninstall(Sdk sdk)
        {
            try
            {
                sdk.IsInstalling = true;
                if (sdk.Installed)
                {
                    var result = await _dotnet.Uninstall(sdk);
                    if (result)
                    {
                        sdk.Path = string.Empty;
                    }
                }
                else
                {
                    var path = await _dotnet.Download(sdk);
                    if (!string.IsNullOrEmpty(path))
                    {
                        var result = await _dotnet.Install(path);
                        if (result)
                        {
                            sdk.Path = await _dotnet.GetInstallationPath(sdk);
                        }
                    }
                }
                sdk.IsInstalling = false;
            }

            catch (Exception ex)
            {
                sdk.IsInstalling = false;
                await _errorHelper.ShowPopup(ex);
            }
        }

        [RelayCommand]
        async Task OpenReleaseNotes()
        {
            try
            { 
                await Browser.Default.OpenAsync(SelectedSdk.Data.ReleaseNotes);
            }
            catch (Exception ex)
            {
                await _errorHelper.ShowPopup(ex);
            }
        }


        [RelayCommand]
        async Task LaunchFileLink(Uri url)
        {
            try
            {
                await Browser.Default.OpenAsync(url);
            }
            catch (Exception ex)
            {
                await _errorHelper.ShowPopup(ex);
            }
        }


        [RelayCommand]
        void ToggleOnline()
        {
            ShowOnline = !ShowOnline;
            Sdks.Search(" ");
            Sdks.Search(_query);
            IsBusy = (!ShowOnline && !ShowInstalled);

        }

        [RelayCommand]
        void ToggleInstalled()
        {
            ShowInstalled = !ShowInstalled;
            Sdks.Search(" ");
            Sdks.Search(_query);
            IsBusy = (!ShowOnline && !ShowInstalled);
        }



        [RelayCommand]
        void ToggleMultiSelection()
        {

        }

        void Sdks_FilterHandler(object sender, ObservableView.Filtering.FilterEventArgs<Sdk> e)
        {
            if(ShowOnline && ShowInstalled)
            {
                e.IsAllowed = true;
            }
            else if(ShowOnline && !ShowInstalled)
            {
                e.IsAllowed = !e.Item.Installed;
            }
            else if(!ShowOnline && ShowInstalled)
            {
                e.IsAllowed = e.Item.Installed;
            }
            else
            {
                e.IsAllowed = false;
            }
        }

        public async Task CheckSdks(bool force = false)
        {
            try
            {
                if(Sdks is not null) Sdks.FilterHandler -= Sdks_FilterHandler;
                IsBusy = true;
                var sdkList = await _dotnet.GetSdks(force);
                Sdks = new ObservableView<Sdk>(sdkList);

                Sdks.SearchSpecification.Add(x => x.VersionDisplay, BinaryOperator.Contains);
                Sdks.SearchSpecification.Add(x => x.Path, BinaryOperator.Contains);
                Sdks.FilterHandler += Sdks_FilterHandler;

                _baseSdks = sdkList;
                LastUpdated = " " + DateTime.Now.ToString("MMMM dd, yyyy HH:mm");
                IsBusy = false;
            }
            catch(Exception ex)
            {
                await _errorHelper.ShowPopup(ex);
            }
        }
    }
}
