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
using Russkyc.DependencyInjection.Interfaces;

namespace MediaClippex.MVVM.ViewModel;

[Service(Scope.Singleton)]
public partial class AboutViewModel : BaseViewModel
{
    private readonly IUpdater _iUpdater;

    [ObservableProperty] private string _checkUpdateButtonContent = "Check for updates";
    [ObservableProperty] private string? _currentVersion;
    [ObservableProperty] private bool _isLatestVersion;
    [ObservableProperty] private bool _isUpdating;
    [ObservableProperty] private double _progress;

    public AboutViewModel(IServicesContainer container)
    {
        _iUpdater = container.Resolve<IUpdater>();
        CurrentVersion = $"Version: {GithubUpdater.ReadCurrentVersion()}";
    }

    [RelayCommand]
    private async Task CheckForUpdate()
    {
        try
        {
            CheckUpdateButtonContent = "Checking for update...";
            var hasUpdate = await _iUpdater.CheckForUpdates();
            IsLatestVersion = !hasUpdate;
            if (IsLatestVersion) return;
            IsUpdating = true;
            await _iUpdater.PerformUpdate(new Progress<double>(val => Progress = val * 100));
        }
        catch (HttpRequestException e)
        {
            MessageBox.Show("Error", e.Message, MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsUpdating = false;
            CheckUpdateButtonContent = "Check for update";
        }
    }
}