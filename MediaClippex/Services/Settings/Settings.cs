using MediaClippex.Services.Settings.Interfaces;
using Microsoft.Win32;
using org.russkyc.moderncontrols.Helpers;
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

    public void ListenToThemeChange(bool data = false)
    {
        SystemEvents.UserPreferenceChanged += (_, e) =>
        {
            if (e.Category != UserPreferenceCategory.General) return;
            data = IsDarkMode();
            ThemeManager.Instance.SetBaseTheme(IsDarkMode() ? "Dark" : "Light");
        };
    }
}