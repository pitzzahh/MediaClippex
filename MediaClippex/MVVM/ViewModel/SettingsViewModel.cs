using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using Material.Icons.WPF;
using MediaClippex.MVVM.Model;
using MediaClippex.Services.Settings.Interfaces;
using Microsoft.Win32;
using org.russkyc.moderncontrols.Helpers;
using Russkyc.DependencyInjection.Attributes;
using Russkyc.DependencyInjection.Enums;

namespace MediaClippex.MVVM.ViewModel;

[Service(Scope.Singleton)]
// ReSharper disable once ClassNeverInstantiated.Global
public partial class SettingsViewModel : BaseViewModel
{
    [ObservableProperty] private bool _isNightMode;
    private bool _nightMode = true;
    [ObservableProperty] private MaterialIcon? _themeIcon;
    [ObservableProperty] private ObservableCollection<ColorData> _themes = new();

    public SettingsViewModel(ISettings settings)
    {
        ThemeManager.Instance
            .GetColorThemes()
            .ToList()
            .ForEach(e => Themes.Add(new ColorData { Color = e }));

        NightMode = settings.IsDarkMode();
        ThemeManager.Instance.SetColorTheme(Themes[3].Color);
        SystemEvents.UserPreferenceChanged += (_, e) =>
        {
            if (e.Category != UserPreferenceCategory.General) return;
            NightMode = settings.IsDarkMode();
        };
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public bool NightMode
    {
        get => _nightMode;
        // ReSharper disable once PropertyCanBeMadeInitOnly.Global
        // ReSharper disable once UnusedMember.Global
        set
        {
            _nightMode = value;
            var materialIcon = new MaterialIcon
            {
                Kind = value ? MaterialIconKind.WeatherSunny : MaterialIconKind.MoonWaxingCrescent
            };
            ThemeIcon = materialIcon;
            OnPropertyChanged();
            ThemeManager.Instance.SetBaseTheme(NightMode ? "Dark" : "Light");
        }
    }

    [RelayCommand]
    private static void ChangeColor(string color)
    {
        ThemeManager.Instance.SetColorTheme(color);
    }
}