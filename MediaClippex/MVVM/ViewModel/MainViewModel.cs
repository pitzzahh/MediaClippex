using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaClippex.Services.Settings.Interfaces;
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

    public MainViewModel(IServicesContainer container, ISettings settings)
    {
        _container = container;
        settings.ListenToThemeChange();
        SettingsViewModel.ChangeColor(ThemeManager.Instance.GetColorThemes().ToList()[3]);
        Context = _container.Resolve<HomeViewModel>();
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