using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaClippex.DB;
using MediaClippex.DB.Core;
using MediaClippex.DB.Persistence;
using MediaClippex.MVVM.View;
using MediaClippex.Services;
using org.russkyc.moderncontrols.Helpers;
using Russkyc.DependencyInjection.Implementations;
using YoutubeExplode.Common;
using YoutubeExplode.Videos.Streams;
using Video = YoutubeExplode.Videos.Video;

namespace MediaClippex.MVVM.ViewModel;

// ReSharper disable once ClassNeverInstantiated.Global
public partial class MediaClippexViewModel : BaseViewModel
{
    [ObservableProperty] private string? _imagePreview;
    [ObservableProperty] private string _title = "MediaClippex ";
    [ObservableProperty] private string _downloadButtonContent = "Download";
    [ObservableProperty] private bool _isDownloading;
    [ObservableProperty] private bool _isProcessing;
    [ObservableProperty] private bool _isProgressIndeterminate;
    [ObservableProperty] private bool _isResolved;
    [ObservableProperty] private ObservableCollection<string> _qualities = new();
    [ObservableProperty] private string? _quality = "Quality";
    [ObservableProperty] private string? _progressInfo;
    [ObservableProperty] private bool _showPreview;
    [ObservableProperty] private bool _hasDownloadHistory;
    [ObservableProperty] private bool _hasQueue;
    [ObservableProperty] private string? _status;
    [ObservableProperty] private ObservableCollection<string> _themes = new();
    [ObservableProperty] private string? _url;

    public ObservableCollection<DownloadedVideoCardViewModel> DownloadedVideoCardViewModels { get; }

    public ObservableCollection<QueuingContentCardViewModel> QueuingContentCardViewModels { get; }

    private readonly List<string> _audioQualities = new();

    private readonly List<string> _videoQualities = new();

    private bool _isAudioOnly;
    private bool _nightMode = true;
    private int _selectedIndex;

    private string _selectedQuality = string.Empty;

    private Video? _video;
    public static IUnitOfWork UnitOfWork { get; private set; } = null!;

    public MediaClippexViewModel()
    {
        UnitOfWork = new UnitOfWork(new MediaClippexDataContext());
        DownloadedVideoCardViewModels = BuilderServices.Resolve<StorageService>().DownloadedVideoCardViewModelsList;
        QueuingContentCardViewModels = BuilderServices.Resolve<StorageService>().QueuingContentCardViewModelsList;
        
        ThemeManager.Instance
            .GetColorThemes()
            .ToList()
            .ForEach(Themes.Add);
        Title += "v" + CheckUpdateViewModel.ReadCurrentVersion();
        Task.Run(GetQueuingVideos);
        Task.Run(GetDownloadedVideos);
    }

    public string SelectedQuality
    {
        get => _selectedQuality;
        set
        {
            _selectedQuality = value;
            GetDownloadSize();
            OnPropertyChanged();
        }
    }

    // ReSharper disable once MemberCanBePrivate.Global
    public bool IsAudioOnly
    {
        get => _isAudioOnly;
        set
        {
            _isAudioOnly = value;
            Qualities.Clear();
            if (_isAudioOnly)
                _audioQualities.ForEach(q => Qualities.Add(q));
            else
                _videoQualities.ForEach(q => Qualities.Add(q));
            SelectedQuality = Qualities.First();
            OnPropertyChanged();
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
            OnPropertyChanged();
            ChangeColorTheme();
        }
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
            ChangeBaseTheme();
        }
    }

    private void ChangeBaseTheme()
    {
        ThemeManager.Instance
            .SetBaseTheme(NightMode ? "Dark" : "Light");
    }

    private void ChangeColorTheme()
    {
        ThemeManager.Instance
            .SetColorTheme(Themes[SelectedIndex]);
    }

    [RelayCommand]
    private async Task Resolve()
    {
        if (string.IsNullOrWhiteSpace(Url))
        {
            MessageBox.Show("Please enter a URL.");
            return;
        }

        if (!StringService.IsYouTubeVideoUrl(Url))
        {
            MessageBox.Show("Please enter a valid YouTube URL.");
            return;
        }

        try
        {
            ProgressInfo = "Processing URL...";
            IsProgressIndeterminate = true;
            IsProcessing = true;
            _video = await VideoService.GetVideo(Url);

            if (_video == null)
            {
                MessageBox.Show("Video not found.");
                return;
            }

            var manifest = await VideoService.GetManifest(Url);

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                if (_video == null) return;
                var videoInfoCardViewModel = BuilderServices.Resolve<VideoInfoCardViewModel>();
                videoInfoCardViewModel.Title = _video.Title;
                videoInfoCardViewModel.ImageUrl = _video.Thumbnails.GetWithHighestResolution().Url;
                videoInfoCardViewModel.Duration =
                    StringService.ConvertToTimeFormat(_video.Duration.GetValueOrDefault());
                videoInfoCardViewModel.Description = _video.Description;
            });
            IsResolved = true;
            InitializeVideoResolutions(manifest);
            GetDownloadSize();
            InitializeAudioResolutions(manifest);
            OnPropertyChanged();
        }
        catch (Exception e)
        {
            MessageBox.Show($"Something went wrong: {e.Message}");
        }
        finally
        {
            ProgressInfo = "";
            IsProgressIndeterminate = false;
            IsProcessing = !IsResolved;
            ShowPreview = true;
        }
    }

    [RelayCommand]
    private void Download()
    {
        if (_video == null) return;
        if (string.IsNullOrWhiteSpace(SelectedQuality))
        {
            MessageBox.Show("Please select a quality.");
            return;
        }

        if (string.IsNullOrWhiteSpace(Url)) return;

        IsDownloading = true;
        IsProcessing = false;
        try
        {
            if (!_video.Duration.HasValue) return;
            var queuingContentCardViewModels = BuilderServices.Resolve<StorageService>().QueuingContentCardViewModelsList;
            queuingContentCardViewModels.Add(new QueuingContentCardViewModel(
                _video.Title,
                StringService.ConvertToTimeFormat(_video.Duration.GetValueOrDefault()),
                _video.Thumbnails.GetWithHighestResolution().Url,
                Url,
                SelectedQuality,
                true,
                IsAudioOnly
            ));
            HasQueue = queuingContentCardViewModels.Count > 0;
        }
        catch (Exception e)
        {
            MessageBox.Show($"Something went wrong: {e.Message}");
        }
        finally
        {
            ProgressInfo = "";
            IsDownloading = false;
            IsProcessing = false;
            ShowPreview = false;
            IsAudioOnly = false;
            IsResolved = false;
        }
    }

    [RelayCommand]
    private static async Task CheckForUpdates()
    {
        var checkUpdateView = BuilderServices.Resolve<CheckUpdateView>();
        if (checkUpdateView.IsVisible) checkUpdateView.Hide();
        checkUpdateView.Show();
        await ((CheckUpdateViewModel)checkUpdateView.DataContext).CheckForUpdate();
    }

    private void InitializeVideoResolutions(StreamManifest manifest)
    {
        if (string.IsNullOrWhiteSpace(Url)) return;
        _videoQualities.Clear();
        Qualities.Clear();
        manifest.GetVideoOnlyStreams()
            .Select(s => s.VideoQuality.Label)
            .Distinct()
            .ToList()
            .ForEach(s => _videoQualities.Add(s));
        _videoQualities.ForEach(q => Qualities.Add(q));
        SelectedQuality = Qualities.First();
    }

    private void InitializeAudioResolutions(StreamManifest manifest)
    {
        if (string.IsNullOrWhiteSpace(Url)) return;
        _audioQualities.Clear();
        manifest.GetAudioOnlyStreams()
            .Select(a => a.Bitrate.ToString())
            .Distinct()
            .ToList()
            .ForEach(s => _audioQualities.Add(s));
    }

    private void GetDownloadSize()
    {
        Task.Run(GetFileSize);
    }

    private async Task GetFileSize()
    {
        if (string.IsNullOrEmpty(Url)) return;
        var manifest = await VideoService.GetManifest(Url);
        var video = await VideoService.GetVideo(Url);
        if (!video.Duration.HasValue) return;
        DownloadButtonContent = IsAudioOnly
            ? $"Download [{VideoService.GetAudioFileSizeFormatted(manifest, video, SelectedQuality)}]"
            : $"Download [{VideoService.GetVideoFileSizeFormatted(manifest, video, SelectedQuality)}]";
    }

    private async Task GetQueuingVideos()
    {
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var queuingContentCardViewModels = BuilderServices.Resolve<StorageService>().QueuingContentCardViewModelsList;
            queuingContentCardViewModels.Clear();
            var enumerable = UnitOfWork.QueuingContentRepository.Find(s => true);
            HasQueue = enumerable.Any();
            foreach (var video in UnitOfWork.QueuingContentRepository.GetAll())
            {
                queuingContentCardViewModels.Add(new QueuingContentCardViewModel(
                    video.Title,
                    video.Duration,
                    video.ThumbnailUrl,
                    video.Url,
                    video.SelectedQuality,
                    !video.Paused
                ));
            }
        });
    }

    private async Task GetDownloadedVideos()
    {
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var downloadedVideoCardViewModels = BuilderServices.Resolve<StorageService>().DownloadedVideoCardViewModelsList;
            downloadedVideoCardViewModels.Clear();
            var enumerable = UnitOfWork.VideosRepository.Find(s => true);
            HasDownloadHistory = enumerable.Any();
            if (!HasDownloadHistory) return;
            foreach (var video in UnitOfWork.VideosRepository.GetAll())
            {
                downloadedVideoCardViewModels.Add(new DownloadedVideoCardViewModel(
                    video.Title,
                    video.Description,
                    video.FileType,
                    string.IsNullOrEmpty(video.Path)
                        ? "Could not determine, file moved to other location"
                        : StringService.ConvertBytesToFormattedString(File.OpenRead(video.Path).Length),
                    video.Path,
                    StringService.ConvertToTimeFormat(StringService.ConvertFromString(video.Duration)),
                    video.ThumbnailUrl
                ));
            }
        });
    }
}