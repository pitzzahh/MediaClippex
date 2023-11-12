using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
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
using MessageBox = System.Windows.MessageBox;

namespace MediaClippex.MVVM.ViewModel;

[Service(Scope.Singleton)]
// ReSharper disable once ClassNeverInstantiated.Global
public partial class SettingsViewModel : BaseViewModel
{
    private readonly IServicesContainer _container;
    private readonly ISettings _settings;
    private readonly IUnitOfWork _unitOfWork;
    [ObservableProperty] private string _downloadPath = string.Empty;
    [ObservableProperty] private bool _isNightMode;
    private bool _nightMode = true;
    [ObservableProperty] private MaterialIcon? _themeIcon;
    [ObservableProperty] private ObservableCollection<ColorData> _themes = new();

    public SettingsViewModel(IServicesContainer container, ISettings settings, IUnitOfWork unitOfWork)
    {
        _container = container;
        _settings = settings;
        _unitOfWork = unitOfWork;
        ThemeManager.Instance
            .GetColorThemes()
            .ToList()
            .ForEach(e => Themes.Add(new ColorData { Color = e }));
        NightMode = settings.IsDarkMode();
        DownloadPath = settings.DownloadPath();
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
    private void ChangeColor(string color)
    {
        ThemeManager.Instance.SetColorTheme(color);
        _settings.ColorTheme(true, color);
    }

    [RelayCommand]
    private void ChangeDownloadPath()
    {
        var folderBrowserDialog = new FolderBrowserDialog
        {
            SelectedPath = DownloadPath,
            ShowNewFolderButton = true
        };

        if (folderBrowserDialog.ShowDialog() != DialogResult.OK) return;

        _settings.DownloadPath(true, folderBrowserDialog.SelectedPath);
        DownloadPath = _settings.DownloadPath();
        MessageBox.Show("Download path changed successfully!", "Success", MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    [RelayCommand]
    private async void ClearData(bool includeFiles)
    {
        var videosRepository = _unitOfWork.VideosRepository;
        var fileCount = videosRepository.GetAll().Count();

        foreach (var video in videosRepository.GetAll())
        {
            videosRepository.Remove(video);
            if (includeFiles) await Task.Run(() => FileHelper.Delete(video.Path));
        }

        if (_unitOfWork.Complete() == fileCount)
        {
            var homeViewModel = _container.Resolve<HomeViewModel>();
            homeViewModel.DownloadedVideoCardViewModels.Clear();
            homeViewModel.HasDownloadHistory = false;
            MessageBox.Show("All data cleared successfully!", "Success", MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        else
        {
            MessageBox.Show("Something went wrong!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}