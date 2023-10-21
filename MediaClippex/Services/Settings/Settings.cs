using MediaClippex.Services.Settings.Interfaces;
using Microsoft.Win32;
using Russkyc.DependencyInjection.Attributes;
using Russkyc.DependencyInjection.Enums;

namespace MediaClippex.Services.Settings;

[Service(Scope.Singleton, Registration.AsInterfaces)]
public class Settings : ISettings
{
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
}