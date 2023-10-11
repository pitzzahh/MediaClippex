using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaClippex.DB.Core;
using MediaClippex.MVVM.Model;
using MediaClippex.MVVM.View;
using MediaClippex.Services;
using org.russkyc.moderncontrols.Helpers;
using Russkyc.DependencyInjection.Implementations;
using YoutubeExplode.Common;
using YoutubeExplode.Playlists;
using YoutubeExplode.Videos.Streams;
using Video = YoutubeExplode.Videos.Video;

namespace MediaClippex.MVVM.ViewModel;

// ReSharper disable once ClassNeverInstantiated.Global
public partial class MediaClippexViewModel : BaseViewModel
{
    private readonly List<string> _audioQualities = new();

    private readonly List<string> _videoQualities = new();
    [ObservableProperty] private string _downloadButtonContent = "Download";

    [ObservableProperty]
    private ObservableCollection<DownloadedVideoCardViewModel> _downloadedVideoCardViewModels = new();

    [ObservableProperty] private bool _hasDownloadHistory;
    [ObservableProperty] private bool _hasQueue;
    [ObservableProperty] private string? _imagePreview;

    private bool _isAudioOnly;
    [ObservableProperty] private bool _isDownloading;
    [ObservableProperty] private bool _isPlaylist;
    [ObservableProperty] private bool _isProcessing;
    [ObservableProperty] private bool _isProgressIndeterminate;
    [ObservableProperty] private bool _isResolved;
    private bool _nightMode = true;
    [ObservableProperty] private string? _progressInfo;
    [ObservableProperty] private ObservableCollection<string> _qualities = new();
    [ObservableProperty] private string? _quality = "Quality";

    [ObservableProperty]
    private ObservableCollection<QueuingContentCardViewModel> _queuingContentCardViewModels = new();

    private IReadOnlyList<PlaylistVideo>? _readOnlyList;

    private int _selectedIndex;

    private string _selectedQuality = string.Empty;
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
            ChangeBaseTheme();
        }
    }

    private void ChangeBaseTheme()
    {
        OnPropertyChanged();
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

        if (IsPlaylist)
        {
            ShowPreview = false;
            _readOnlyList = await VideoService.GetPlaylistVideos(Url);
            if (_readOnlyList.Count == 0)
            {
                MessageBox.Show("Playlist not found.", "Cannot resolve", MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var playlistInfo = await VideoService.GetPlaylistInfo(Url);
            var streamManifest = await VideoService.GetVideoManifest(_readOnlyList[0].Url);

            ShowPreview = true;
            IsProgressIndeterminate = false;
            IsProcessing = false;
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var videoInfoCardViewModel = BuilderServices.Resolve<VideoInfoCardViewModel>();
                videoInfoCardViewModel.Title = playlistInfo.Title;
                videoInfoCardViewModel.ImageUrl = playlistInfo.Thumbnails.GetWithHighestResolution().Url;
                videoInfoCardViewModel.Duration = "N/A";
                videoInfoCardViewModel.Description = playlistInfo.Description;
            });

            IsResolved = true;
            InitializeVideoResolutions(streamManifest);
            GetDownloadSize();
            InitializeAudioResolutions(streamManifest);
            OnPropertyChanged();
        }
        else
        {
            try
            {
                ShowPreview = false;
                _video = await VideoService.GetVideo(Url);

                if (_video == null)
                {
                    MessageBox.Show("Video not found.", "Cannot resolve", MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                var manifest = await VideoService.GetVideoManifest(Url);
                ShowPreview = true;
                IsProgressIndeterminate = false;
                IsProcessing = false;
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
                MessageBox.Show($"Something went wrong resolving the url: {e.Message}", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                IsProcessing = false;
                IsProgressIndeterminate = false;
                ProgressInfo = "";
            }
        }
    }

    // TODO: refactor this, it's inefficient
    [RelayCommand]
    private void Download()
    {
        if (_video == null) return;
        if (_readOnlyList == null) return;

        if (string.IsNullOrWhiteSpace(SelectedQuality))
        {
            MessageBox.Show("Please select a quality.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(Url)) return;

        IsDownloading = true;
        IsProcessing = false;


        if (IsPlaylist)
            foreach (var playlistVideo in _readOnlyList)
            {
                MessageBox.Show($"{playlistVideo}");
                try
                {
                    if (!playlistVideo.Duration.HasValue) return;
                    var queuingContentCardViewModel = new QueuingContentCardViewModel(
                        playlistVideo.Title,
                        StringService.ConvertToTimeFormat(playlistVideo.Duration.GetValueOrDefault()),
                        playlistVideo.Thumbnails.GetWithHighestResolution().Url,
                        playlistVideo.Url,
                        SelectedQuality,
                        true,
                        IsAudioOnly,
                        "N/A"
                    );
                    QueuingContentCardViewModels.Add(queuingContentCardViewModel);
                    // Adding to QueuingContent Table
                    UnitOfWork.QueuingContentRepository.Add(
                        new QueuingContent(
                            playlistVideo.Title,
                            queuingContentCardViewModel.Duration,
                            playlistVideo.Thumbnails.GetWithHighestResolution().Url,
                            IsAudioOnly ? "Audio" : "Video",
                            Url,
                            0,
                            "Just Started",
                            SelectedQuality,
                            false,
                            IsAudioOnly)
                    );
                    Url = "";
                    var addedToQueueDb = UnitOfWork.Complete();
                    if (addedToQueueDb == 0) return;
                    HasQueue = true;
                }
                catch (Exception e)
                {
                    MessageBox.Show($"Something went wrong downloading: {e.Message}", "Error", MessageBoxButton.OK,
                        MessageBoxImage.Error);
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
        else
            try
            {
                if (!_video.Duration.HasValue) return;
                var queuingContentCardViewModel = new QueuingContentCardViewModel(
                    _video.Title,
                    StringService.ConvertToTimeFormat(_video.Duration.GetValueOrDefault()),
                    _video.Thumbnails.GetWithHighestResolution().Url,
                    Url,
                    SelectedQuality,
                    true,
                    IsAudioOnly,
                    _video.Description
                );
                QueuingContentCardViewModels.Add(queuingContentCardViewModel);
                // Adding to QueuingContent Table
                UnitOfWork.QueuingContentRepository.Add(
                    new QueuingContent(
                        queuingContentCardViewModel.Title,
                        queuingContentCardViewModel.Duration,
                        queuingContentCardViewModel.ThumbnailUrl,
                        IsAudioOnly ? "Audio" : "Video",
                        Url,
                        0,
                        "Just Started",
                        SelectedQuality,
                        false,
                        IsAudioOnly)
                );
                Url = "";
                var addedToQueueDb = UnitOfWork.Complete();
                if (addedToQueueDb == 0) return;
                HasQueue = true;
            }
            catch (Exception e)
            {
                MessageBox.Show($"Something went wrong downloading: {e.Message}", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
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
        var manifest = await VideoService.GetVideoManifest(Url);
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
            QueuingContentCardViewModels.Clear();
            var queuingVideos = UnitOfWork.QueuingContentRepository.GetAll().ToList();
            HasQueue = queuingVideos.Count > 0;
            if (!HasQueue) return;
            try
            {
                foreach (var video in queuingVideos)
                    QueuingContentCardViewModels.Add(new QueuingContentCardViewModel(
                        video.Title,
                        video.Duration,
                        video.ThumbnailUrl,
                        video.Url,
                        video.SelectedQuality,
                        !video.Paused
                    ));
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message, "Error loading Queues", MessageBoxButton.OK, MessageBoxImage.Error);
                throw;
            }
        });
    }

    public async Task GetDownloadedVideos()
    {
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            DownloadedVideoCardViewModels.Clear();
            var videos = UnitOfWork.VideosRepository.GetAll().ToList();
            HasDownloadHistory = videos.Count > 0;
            if (!HasDownloadHistory) return;

            foreach (var video in videos)
            {
                string fileSize;
                try
                {
                    fileSize = video.Path is null
                        ? "Cannot be determined"
                        : StringService.ConvertBytesToFormattedString(new FileInfo(video.Path).Length);
                }
                catch (Exception)
                {
                    fileSize = "Cannot be determined";
                }

                DownloadedVideoCardViewModels.Add(new DownloadedVideoCardViewModel(
                    video.Title,
                    video.Description,
                    video.FileType,
                    fileSize,
                    video.Path,
                    video.Duration,
                    video.ThumbnailUrl
                ));
            }
        });
    }
}