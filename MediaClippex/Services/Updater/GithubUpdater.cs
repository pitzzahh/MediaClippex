using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using MediaClippex.Services.Updater.Interfaces;
using Onova;
using Onova.Services;
using Russkyc.DependencyInjection.Attributes;
using Russkyc.DependencyInjection.Enums;

namespace MediaClippex.Services.Updater;

[Service(Scope.Singleton, Registration.AsInterfaces)]
public class GithubUpdater : IUpdater
{
    private readonly IUpdateManager _updateManager = new UpdateManager(
        new GithubPackageResolver(
            "pitzzahh",
            "MediaClippex",
            "mediaclippex-standalone-*.zip"
        ),
        new ZipPackageExtractor()
    );

    private Version? _latestVersion;

    public string GetLatestVersion()
    {
        return $"{_latestVersion!.Major}.{_latestVersion.Minor}.{_latestVersion.Build}";
    }

    public async Task<bool> CheckForUpdates()
    {
        var result = await _updateManager.CheckForUpdatesAsync();
        if (result.LastVersion == null) return false;

        var readCurrentVersion = ReadCurrentVersion();

        if (readCurrentVersion == null) return false;

        _latestVersion = result.LastVersion;

        if (ShouldUpdate(readCurrentVersion, _latestVersion))
            return MessageBox.Show("Update available", "New update available. Update to a new version",
                MessageBoxButton.OKCancel, MessageBoxImage.Information) == MessageBoxResult.OK;
        MessageBox.Show("No update", "You have the latest version of the app", MessageBoxButton.OK,
            MessageBoxImage.Information);
        return false;
    }

    public async Task PerformUpdate(IProgress<double> progress)
    {
        if (_latestVersion is null) return;
        await _updateManager.PrepareUpdateAsync(_latestVersion, progress);
        await Task.Run(() => _updateManager.LaunchUpdater(_latestVersion));
        Environment.Exit(0);
    }

    public static Version? ReadCurrentVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version;
    }

    private static bool ShouldUpdate(Version currentVersion, Version latestVersion)
    {
        return latestVersion > currentVersion;
    }
}