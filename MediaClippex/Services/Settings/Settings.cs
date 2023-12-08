using System;
using System.IO;
using MediaClippex.Services.Config.Interfaces;
using MediaClippex.Services.Helpers;
using MediaClippex.Services.Settings.Interfaces;
using Microsoft.Win32;
using org.russkyc.moderncontrols.Helpers;
using Russkyc.DependencyInjection.Attributes;
using Russkyc.DependencyInjection.Enums;

namespace MediaClippex.Services.Settings;

[Service(Scope.Singleton, Registration.AsInterfaces)]
public class Settings : ISettings
{
    private readonly IConfig _config;

    public Settings(IConfig config)
    {
        _config = config;
    }

    public bool IsDarkMode()
    {
        try
        {
            return Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                false
            )?.GetValue("AppsUseLightTheme") is 0;
        }
        catch
        {
            return false;
        }
    }

    public string ColorTheme(bool change = false, string colorTheme = "")
    {
        if (change) _config.ColorTheme = colorTheme;
        return _config.ColorTheme;
    }

    public string DownloadPath(bool change = false, string downloadPath = "")
    {
        if (change) _config.DownloadPath = downloadPath;
        var path = _config.DownloadPath;
        return Path.GetFullPath(Path.GetFullPath(path.Contains("%USERPROFILE%")
            ? Environment.ExpandEnvironmentVariables(path)
            : Path.Combine(path, DirectoryService.CreateDirectoryIfNotPresent("MediaClippex")
            ))
        );
    }

    public int QueryResultLimit(bool change = false, int queryResultLimit = 30)
    {
        if (change) _config.QueryResultLimit = queryResultLimit;
        return _config.QueryResultLimit;
    }

    public void ListenToThemeChange(bool data = false)
    {
        SystemEvents.UserPreferenceChanged += (_, e) =>
        {
            if (e.Category != UserPreferenceCategory.General) return;
            data = IsDarkMode();
            ThemeManager.Instance.SetBaseTheme(data ? "Dark" : "Light");
        };
    }
}