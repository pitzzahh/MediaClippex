using MediaClippex.Services.Config.Interfaces;
using Russkyc.Configuration;
using Russkyc.DependencyInjection.Attributes;
using Russkyc.DependencyInjection.Enums;

namespace MediaClippex.Services.Config;

[Service(Scope.Singleton, Registration.AsInterfaces)]
public class Config : ConfigProvider, IConfig
{
    public Config() : base("config.json")
    {
    }

    public string ColorTheme
    {
        get => GetValue<string>(nameof(ColorTheme));
        set => SetValue(nameof(ColorTheme), value);
    }

    public string DownloadPath
    {
        get => GetValue<string>(nameof(DownloadPath));
        set => SetValue(nameof(DownloadPath), value);
    }
}