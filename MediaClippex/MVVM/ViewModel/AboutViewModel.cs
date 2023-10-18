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
            var hasUpdate = await _updater.CheckForUpdates();
            IsLatestVersion = !hasUpdate;
            if (IsLatestVersion) return;
            IsUpdating = true;
            await _updater.PerformUpdate(new Progress<double>(val => Progress = val * 100));
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