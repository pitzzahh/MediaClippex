using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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
    private int _selectedIndex;
    [ObservableProperty] private ObservableCollection<string> _themes = new();

    public MainViewModel(IServicesContainer container)
    {
        _container = container;
        ThemeManager.Instance
            .GetColorThemes()
            .ToList()
            .ForEach(Themes.Add);
        SelectedIndex = 3;
        Context = _container.Resolve<HomeViewModel>();
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
    private void NavigateToHome() // violates dry principle, will fix soon
    {
        var viewModel = _container.Resolve<HomeViewModel>();
        if (Context == viewModel) return;
        Context = viewModel;
    }

    [RelayCommand]
    private void NavigateToAbout() // violates dry principle, will fix soon (also violates dry principle, the comment)
    {
        var viewModel = _container.Resolve<AboutViewModel>();
        if (Context == viewModel) return;
        Context = viewModel;
    }

    [RelayCommand]
    private void
        NavigateToSettings() // violates dry principle, will fix soon (also violates dry principle, the comment)
    {
        var viewModel = _container.Resolve<SettingsViewModel>();
        if (Context == viewModel) return;
        Context = viewModel;
    }
}