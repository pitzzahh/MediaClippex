namespace MediaClippex.Services.Settings.Interfaces;

public interface ISettings
{
    bool IsDarkMode();
    void ListenToThemeChange(bool data = false);
}