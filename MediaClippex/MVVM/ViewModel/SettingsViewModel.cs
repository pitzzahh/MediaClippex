using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Material.Icons;
using Material.Icons.WPF;
using MediaClippex.DB.Core;
using MediaClippex.Helpers;
using MediaClippex.MVVM.Model;
using MediaClippex.Services.Settings.Interfaces;
using org.russkyc.moderncontrols.Helpers;
using Russkyc.DependencyInjection.Attributes;
using Russkyc.DependencyInjection.Enums;
using Russkyc.DependencyInjection.Interfaces;

namespace MediaClippex.MVVM.ViewModel;

[Service(Scope.Singleton)]
// ReSharper disable once ClassNeverInstantiated.Global
public partial class SettingsViewModel : BaseViewModel
{
    private readonly HomeViewModel _homeViewModel;
    private readonly IUnitOfWork _unitOfWork;
    [ObservableProperty] private bool _isNightMode;
    private bool _nightMode = true;
    [ObservableProperty] private MaterialIcon? _themeIcon;
    [ObservableProperty] private ObservableCollection<ColorData> _themes = new();

    public SettingsViewModel(IServicesContainer container, ISettings settings, IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _homeViewModel = container.Resolve<HomeViewModel>();
        ThemeManager.Instance
            .GetColorThemes()
            .ToList()
            .ForEach(e => Themes.Add(new ColorData { Color = e }));

        NightMode = settings.IsDarkMode();
        ThemeManager.Instance.SetColorTheme(Themes[3].Color);
        settings.ListenToThemeChange(NightMode);
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

    [RelayCommand]
    private async void ClearData(bool includeFiles)
    {
        var videosRepository = _unitOfWork.VideosRepository;
        var fileCount = videosRepository.GetAll().Count();

        foreach (var video in videosRepository.GetAll())
        {
            videosRepository.Remove(video);
            _homeViewModel.DownloadedVideoCardViewModels.Remove(
                _homeViewModel.DownloadedVideoCardViewModels
                    .First(e => e.Title == video.Title && e.Path == video.Path && e.FileSize == video.FileSize)
            );
            if (includeFiles) await Task.Run(() => FileHelper.Delete(video.Path));
        }

        if (_unitOfWork.Complete() == fileCount)
        {
            _homeViewModel.HasDownloadHistory = _homeViewModel.DownloadedVideoCardViewModels.Count > 0;
            MessageBox.Show("All data cleared successfully!", "Success", MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show("Something went wrong!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}