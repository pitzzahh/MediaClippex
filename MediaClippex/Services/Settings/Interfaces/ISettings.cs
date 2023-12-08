namespace MediaClippex.Services.Settings.Interfaces;

public interface ISettings
{
    bool IsDarkMode();
    string ColorTheme(bool change = false, string colorTheme = "");
    void ListenToThemeChange(bool data = false);
    string DownloadPath(bool change = false, string downloadPath = "");
    int QueryResultLimit(bool change = false, int queryResultLimit = 30);
}