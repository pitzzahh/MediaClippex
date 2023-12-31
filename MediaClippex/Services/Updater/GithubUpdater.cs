﻿using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using MediaClippex.Services.Helpers;
using MediaClippex.Services.Updater.Interfaces;
using Onova;
using Onova.Services;
using Russkyc.DependencyInjection.Attributes;
using Russkyc.DependencyInjection.Enums;

namespace MediaClippex.Services.Updater;

[Service(Scope.Singleton, Registration.AsInterfaces)]
// ReSharper disable once ClassNeverInstantiated.Global
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

    public async Task<UpdateStatus> CheckForUpdates()
    {
        var result = await _updateManager.CheckForUpdatesAsync();
        if (result.LastVersion is null) return UpdateStatus.NoAsset;

        var readCurrentVersion = ReadCurrentVersion();

        if (readCurrentVersion is null) return UpdateStatus.Error;

        _latestVersion = result.LastVersion;

        return ShouldUpdate(readCurrentVersion, _latestVersion) ? UpdateStatus.Available : UpdateStatus.NotAvailable;
    }

    public async Task PerformUpdate(IProgress<double> progress)
    {
        if (_latestVersion is null) return;
        FileUtil.Copy(
            Path.Combine(AppContext.BaseDirectory, "config.json"),
            Path.Combine(AppContext.BaseDirectory, "config.json.bak"),
            true
        );
        await _updateManager.PrepareUpdateAsync(_latestVersion, progress);
        _updateManager.LaunchUpdater(_latestVersion);
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