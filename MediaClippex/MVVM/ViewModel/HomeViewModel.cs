using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaClippex.DB.Core;
using MediaClippex.Services;
using org.russkyc.moderncontrols.Helpers;
using Russkyc.DependencyInjection.Attributes;
using Russkyc.DependencyInjection.Enums;
using Russkyc.DependencyInjection.Interfaces;
using YoutubeExplode.Common;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;

namespace MediaClippex.MVVM.ViewModel;

[Service(registration: Registration.AsInterfaces)]
public partial class HomeViewModel : BaseViewModel
{
    private readonly IServicesContainer _container;

    [ObservableProperty]
    private ObservableCollection<DownloadedContentCardViewModel> _downloadedVideoCardViewModels = new();

    [ObservableProperty] private bool _hasDownloadHistory;
    [ObservableProperty] private bool _hasQueue;
    [ObservableProperty] private string? _imagePreview;

    [ObservableProperty] private bool _isDownloading;
    [ObservableProperty] private bool _isPlaylist;
    [ObservableProperty] private bool _isProcessing;
    [ObservableProperty] private bool _isProgressIndeterminate;
    [ObservableProperty] private bool _isResolved;

    private bool _nightMode = true;

    [ObservableProperty] private ObservableCollection<PreviewCardViewModel> _previewCardViewModels = new();
    [ObservableProperty] private string? _progressInfo;

    [ObservableProperty]
    private ObservableCollection<QueuingContentCardViewModel> _queuingContentCardViewModels = new();

    private IReadOnlyList<PlaylistVideo>? _readOnlyList;

    [ObservableProperty] private bool _showPreview;
    [ObservableProperty] private string? _status;

    [ObservableProperty] private string _title = "MediaClippex ";
    [ObservableProperty] private string? _url;

    private Video? _video;

    public HomeViewModel(IServicesContainer container)
    {
        _container = container;
        UnitOfWork = container.Resolve<IUnitOfWork>();
        NightMode = SettingsService.IsDarkModeEnabledByDefault();
        Task.Run(GetQueuingVideos);
        Task.Run(GetDownloadedVideos);
    }

    private static IUnitOfWork UnitOfWork { get; set; } = null!;


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

    [RelayCommand]
    private async Task Resolve()
    {
        if (string.IsNullOrWhiteSpace(Url))
        {
            MessageBox.Show("Please enter a URL.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!StringService.IsYouTubeVideoUrl(Url) && !StringService.IsYouTubePlaylistUrl(Url))
        {
            MessageBox.Show("Please enter a valid YouTube URL.", "Warning", MessageBoxButton.OK,
                MessageBoxImage.Warning);
            Url = "";
            return;
        }

        IsPlaylist = StringService.IsYouTubePlaylistUrl(Url);

        ProgressInfo = "Processing URL...";
        IsProgressIndeterminate = true;
        IsProcessing = true;

        try
        {
            if (IsPlaylist)
            {
                PreviewCardViewModels.Clear();
                _readOnlyList = await VideoService.GetPlaylistVideos(Url);

                if (_readOnlyList.Count == 0)
                {
                    MessageBox.Show("Playlist not found.", "Cannot resolve", MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                IsProcessing = false;
                ShowPreview = true;
                IsProgressIndeterminate = false;

                foreach (var playlistVideo in _readOnlyList)
                    PreviewCardViewModels.Add(new PreviewCardViewModel(
                        _container,
                        playlistVideo.Title,
                        StringService.ConvertToTimeFormat(playlistVideo.Duration.GetValueOrDefault()),
                        playlistVideo.Author.ChannelTitle,
                        playlistVideo.Thumbnails.GetWithHighestResolution().Url,
                        playlistVideo.Url
                    ));
            }
            else
            {
                _video = await VideoService.GetVideo(Url);

                if (_video == null)
                {
                    MessageBox.Show("Video not found.", "Cannot resolve", MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                IsProcessing = false;
                ShowPreview = true;
                IsProgressIndeterminate = false;

                PreviewCardViewModels.Add(new PreviewCardViewModel(
                    _container,
                    _video.Title,
                    StringService.ConvertToTimeFormat(_video.Duration.GetValueOrDefault()),
                    _video.Author.ChannelTitle,
                    _video.Thumbnails.GetWithHighestResolution().Url,
                    Url
                ));
            }
        }
        catch (Exception e)
        {
            MessageBox.Show($"Something went wrong resolving: {e.Message}", "Error", MessageBoxButton.OK,
                MessageBoxImage.Error);
            ShowPreview = false;
            IsProcessing = false;
        }
        finally
        {
            Url = "";
        }
    }

    private void GetQueuingVideos()
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            QueuingContentCardViewModels.Clear();
            var queuingVideos = UnitOfWork.QueuingContentRepository.GetAll().Reverse().ToList();
            HasQueue = queuingVideos.Count > 0;
            if (!HasQueue) return;
            foreach (var video in queuingVideos)
                QueuingContentCardViewModels.Add(new QueuingContentCardViewModel(
                    _container,
                    video.Title,
                    video.Duration,
                    video.ThumbnailUrl,
                    video.Url,
                    video.SelectedQuality,
                    !video.Paused
                ));
        });
    }

    private void GetDownloadedVideos()
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            DownloadedVideoCardViewModels.Clear();
            var videos = UnitOfWork.VideosRepository.GetAll().Reverse().ToList();
            HasDownloadHistory = videos.Count > 0;
            if (!HasDownloadHistory) return;

            foreach (var video in videos)
            {
                DownloadedVideoCardViewModels.Add(new DownloadedContentCardViewModel(
                    _container,
                    video.Title,
                    video.FileType,
                    video.FileSize,
                    video.Path,
                    video.Duration,
                    video.ThumbnailUrl
                ));
            }
        });
    }
}