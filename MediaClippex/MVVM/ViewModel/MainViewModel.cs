using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaClippex.Services;
using org.russkyc.moderncontrols.Helpers;
using Russkyc.DependencyInjection.Attributes;
using Russkyc.DependencyInjection.Enums;
using Russkyc.DependencyInjection.Interfaces;

namespace MediaClippex.MVVM.ViewModel;

[Service(Scope.Singleton)]
public partial class MainViewModel : BaseViewModel
{
    private readonly IServicesContainer _container;
    [ObservableProperty] private BaseViewModel? _context;
    [ObservableProperty] private bool _isHome;
    [ObservableProperty] private bool _isNightMode;
    private int _selectedIndex;
    private bool _nightMode = true;
    [ObservableProperty] private ObservableCollection<string> _themes = new();

    public MainViewModel(IServicesContainer container)
    {
        _container = container;
        ThemeManager.Instance
            .GetColorThemes()
            .ToList()
            .ForEach(Themes.Add);
        SelectedIndex = 3;
        IsHome = true;
        Context = _container.Resolve<HomeViewModel>();
        NightMode = SettingsService.IsDarkModeEnabledByDefault();
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
            OnPropertyChanged();
            ThemeManager.Instance.SetBaseTheme(NightMode ? "Dark" : "Light");
        }
    }
    
    // ReSharper disable once MemberCanBePrivate.Global
    public int SelectedIndex
    {
        get => _selectedIndex;
        // ReSharper disable once PropertyCanBeMadeInitOnly.Global
        // ReSharper disable once UnusedMember.Global
        set
        {
            _selectedIndex = value;
            ThemeManager.Instance.SetColorTheme(Themes[SelectedIndex]);
        }
    }

    [RelayCommand]
    private void NavigateToHome() // violates dry, will fix soon
    {
        var homeViewModel = _container.Resolve<HomeViewModel>();
        if (Context == homeViewModel) return;

        IsHome = true;
        Context = homeViewModel;
    }

    [RelayCommand]
    private void NavigateToAbout() // violates dry, will fix soon (also violates dry, the comment)
    {
        var aboutViewModel = _container.Resolve<AboutViewModel>();
        if (Context == aboutViewModel) return;
        IsHome = false;
        Context = aboutViewModel;
    }
    
    [RelayCommand]
    private void NavigateToSettings() // violates dry, will fix soon (also violates dry, the comment)
    {
        var aboutViewModel = _container.Resolve<AboutViewModel>();
        if (Context == aboutViewModel) return;
        IsHome = false;
        Context = aboutViewModel;
    }
}