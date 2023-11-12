using System;
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
        ThemeManager.Instance.SetBaseTheme(settings.IsDarkMode() ? "Dark" : "Light");
        ThemeManager.Instance.SetColorTheme(settings.ColorTheme());
        Navigate(typeof(HomeViewModel));
    }

    [RelayCommand]
    private void Navigate(object parameter)
    {
        if (parameter is not Type type) return;
        var viewModel = _container.Resolve(type);
        if (viewModel == null || Context == viewModel) return;
        Context = (BaseViewModel?)viewModel;
    }
}