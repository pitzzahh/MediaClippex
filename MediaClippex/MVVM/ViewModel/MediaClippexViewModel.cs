using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaClippex.DB.Core;
using MediaClippex.MVVM.View;
using MediaClippex.Services;
using org.russkyc.moderncontrols.Helpers;
using Russkyc.DependencyInjection.Implementations;
using YoutubeExplode.Common;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos;

namespace MediaClippex.MVVM.ViewModel;

// ReSharper disable once ClassNeverInstantiated.Global
public partial class MediaClippexViewModel : BaseViewModel
{
    [ObservableProperty]
    private ObservableCollection<DownloadedVideoCardViewModel> _downloadedVideoCardViewModels = new();

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

    private int _selectedIndex;

    [ObservableProperty] private bool _showPreview;
    [ObservableProperty] private string? _status;
    [ObservableProperty] private ObservableCollection<string> _themes = new();
    [ObservableProperty] private string _title = "MediaClippex ";
    [ObservableProperty] private string? _url;

    private Video? _video;

    public MediaClippexViewModel(IUnitOfWork unitOfWork)
    {
        UnitOfWork = unitOfWork;
        ThemeManager.Instance
            .GetColorThemes()
            .ToList()
            .ForEach(Themes.Add);
        Title += "v" + CheckUpdateViewModel.ReadCurrentVersion();
        SelectedIndex = 3;
        NightMode = SettingsService.IsDarkModeEnabledByDefault();
        Task.Run(GetQueuingVideos);
        Task.Run(GetDownloadedVideos);
        CheckUpdateViewModel.InitCheckUpdate();
    }

    private static IUnitOfWork UnitOfWork { get; set; } = null!;


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

    [RelayCommand]
    private static async Task CheckForUpdates()
    {
        var checkUpdateView = BuilderServices.Resolve<CheckUpdateView>();
        if (checkUpdateView.IsVisible) checkUpdateView.Hide();
        checkUpdateView.Show();
        await ((CheckUpdateViewModel)checkUpdateView.DataContext).CheckForUpdate();
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
                DownloadedVideoCardViewModels.Add(new DownloadedVideoCardViewModel(
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