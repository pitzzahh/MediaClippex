using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaClippex.Services.Updater;
using MediaClippex.Services.Updater.Interfaces;
using Russkyc.DependencyInjection.Attributes;
using Russkyc.DependencyInjection.Enums;

namespace MediaClippex.MVVM.ViewModel;

[Service(Scope.Singleton)]
public partial class AboutViewModel : BaseViewModel
{
    private readonly IUpdater _updater;

    [ObservableProperty] private string _checkUpdateButtonContent = "Check for updates";
    [ObservableProperty] private string? _currentVersion;
    [ObservableProperty] private bool _isLatestVersion;
    [ObservableProperty] private bool _isUpdating;
    [ObservableProperty] private double _progress;

    public AboutViewModel(IUpdater updater)
    {
        _updater = updater;
        CurrentVersion = $"Version: {GithubUpdater.ReadCurrentVersion()}";
    }

    [RelayCommand]
    private async Task CheckForUpdate()
    {
        try
        {
            CheckUpdateButtonContent = "Checking for update...";
            var status = await _updater.CheckForUpdates();
            IsLatestVersion = status is UpdateStatus.NotAvailable or UpdateStatus.NoAsset or UpdateStatus.Error;
            switch (status)
            {
                case UpdateStatus.Available:
                    var result = MessageBox.Show("New update available. Update to a new version", "Update available",
                        MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (result == MessageBoxResult.Yes)
                    {
                        IsUpdating = true;
                        CurrentVersion = $"New version after installation: {_updater.GetLatestVersion()}";
                        await _updater.PerformUpdate(new Progress<double>(val => Progress = val * 100));
                    }

                    break;
                case UpdateStatus.NotAvailable:
                    MessageBox.Show("You have the latest version of the app", "No update", MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    break;
                case UpdateStatus.NoAsset:
                    MessageBox.Show("No packages found or\nYou have the latest version", "Info", MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    break;
                case UpdateStatus.Error:
                    MessageBox.Show("Cannot read current version", "Read error", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        catch (HttpRequestException e)
        {
            MessageBox.Show(e.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsUpdating = false;
            CheckUpdateButtonContent = "Check for update";
        }
    }
}