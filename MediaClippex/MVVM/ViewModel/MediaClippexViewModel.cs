using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaClippex.DB;
using MediaClippex.DB.Core;
using MediaClippex.DB.Persistence;
using MediaClippex.Helpers;
using MediaClippex.MVVM.View;
using MediaClippex.Services;
using org.russkyc.moderncontrols.Helpers;
using Russkyc.DependencyInjection.Implementations;
using YoutubeExplode.Common;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace MediaClippex.MVVM.ViewModel;

// ReSharper disable once ClassNeverInstantiated.Global
public partial class MediaClippexViewModel : BaseViewModel
{
    private readonly List<string> _audioQualities = new();

    private readonly List<string> _videoQualities = new();

    [ObservableProperty] private string? _imagePreview;
    private bool _isAudioOnly;

    [ObservableProperty] private string _downloadButtonContent = "Download";
    [ObservableProperty] private bool _isDownloading;

    [ObservableProperty] private bool _isProcessing;

    [ObservableProperty] private bool _isProgressIndeterminate;

    [ObservableProperty] private bool _isResolved;
    private bool _nightMode = true;

    [ObservableProperty] private double _progress;

    [ObservableProperty] private string? _progressInfo;

    [ObservableProperty] private ObservableCollection<string> _qualities = new();

    [ObservableProperty] private string? _quality = "Quality";

    private int _selectedIndex;

    private string _selectedQuality = string.Empty;

    [ObservableProperty] private bool _showPreview;
    [ObservableProperty] private bool _hasDownloadHistory;

    [ObservableProperty] private string? _status;

    [ObservableProperty] private ObservableCollection<string> _themes = new();

    [ObservableProperty] private string? _url;

    [ObservableProperty]
    private ObservableCollection<DownloadedVideoCardViewModel> _downloadedVideoCardViewModels = new();

    private CancellationTokenSource _cancellationTokenSource = null!;

    private Video? _video;
    public static Window UpdateWindow = new CheckUpdateView();
    private IUnitOfWork UnitOfWork { get; }


    public MediaClippexViewModel()
    {
        UnitOfWork = new UnitOfWork(new MediaClippexDataContext());
        ThemeManager.Instance
            .GetColorThemes()
            .ToList()
            .ForEach(Themes.Add);
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
    private async Task Download()
    {
        if (_video == null) return;
        if (string.IsNullOrWhiteSpace(SelectedQuality))
        {
            MessageBox.Show("Please select a quality.");
            return;
        }

        var fixedFileName = $"{StringService.FixFileName(_video.Title)}";

        var videoFilePath = Path.Combine(DirectoryHelper.GetVideoSavingDirectory(), fixedFileName);
        var audioFilePath = Path.Combine(DirectoryHelper.GetAudioSavingDirectory(), fixedFileName);

        if (string.IsNullOrWhiteSpace(Url)) return;

        ProgressInfo = "Downloading...";
        IsDownloading = true;
        Progress = 0;
        IsProcessing = true;

        try
        {
            string savedPath;
            _cancellationTokenSource = new();
            var progressHandler = new Progress<double>(p => Progress = p * 100);
            var manifest = await VideoService.GetManifest(Url);
            if (!_video.Duration.HasValue) return;
            var cancellationToken = _cancellationTokenSource.Token;
            if (IsAudioOnly)
            {
                var audioStreamInfo = VideoService.GetAudioOnlyStream(manifest, SelectedQuality);

                await VideoService.DownloadAudioOnly(audioStreamInfo, audioFilePath, progressHandler,
                    cancellationToken);
                savedPath = $"{audioFilePath}.mp3";
            }
            else
            {
                var audioStreamInfo = manifest.GetAudioOnlyStreams().GetWithHighestBitrate();
                var videoStreamInfo = VideoService.GetVideoOnlyStreamInfo(manifest, SelectedQuality);
                await VideoService.DownloadMuxed(audioStreamInfo, videoStreamInfo, videoFilePath, progressHandler,
                    cancellationToken);
                savedPath = $"{videoFilePath}.{videoStreamInfo.Container}";
            }

            var fileSize = await GetFileSize();

            var thumbNail = _video.Thumbnails.GetWithHighestResolution().Url;

            var videoData = new Model.Video(
                thumbNail,
                _video.Title,
                _video.Duration.Value.TotalSeconds.ToString(CultureInfo.CurrentCulture),
                _video.Description,
                IsAudioOnly ? "Audio" : "Video",
                fileSize,
                savedPath
            );

            UnitOfWork.VideosRepository.Add(videoData);

            var complete = UnitOfWork.Complete();

            if (complete != 0)
            {
                Qualities.Clear();
                SelectedQuality = "";
                IsAudioOnly = false;
                DownloadButtonContent = "Download";
                MessageBox.Show(IsAudioOnly
                    ? $"Audio downloaded successfully. Saved to {audioFilePath}"
                    : $"Video downloaded successfully. Saved to {videoFilePath}");
                await Task.Run(GetDownloadedVideos, cancellationToken);
            }
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
            Progress = 0;
            IsAudioOnly = false;
            IsResolved = false;
        }
    }

    [RelayCommand]
    private static async Task CheckForUpdates()
    {
        if (UpdateWindow.IsVisible) UpdateWindow.Close();
        UpdateWindow = new CheckUpdateView();
        UpdateWindow.Show();
        await ((CheckUpdateViewModel)UpdateWindow.DataContext).CheckForUpdate();
    }

    [RelayCommand]
    private void CancelDownload()
    {
        var messageBoxResult = MessageBox.Show("Do you want to cancel the download?", "Cancel Download",
            MessageBoxButton.YesNo);
        if (messageBoxResult != MessageBoxResult.Yes) return;
        _cancellationTokenSource.Cancel();
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
        Task.Run(async () => { await GetFileSize(); });
    }

    private async Task<string> GetFileSize()
    {
        if (string.IsNullOrEmpty(Url)) return string.Empty;
        var manifest = await VideoService.GetManifest(Url);
        var video = await VideoService.GetVideo(Url);

        if (!video.Duration.HasValue) return string.Empty;

        if (IsAudioOnly)
        {
            var audioStreamInfo = VideoService.GetAudioOnlyStream(manifest, SelectedQuality);

            var bitsPerSecond = audioStreamInfo.Bitrate.BitsPerSecond;

            var duration = video.Duration.Value.TotalSeconds;
            var fileSize = (long)(bitsPerSecond * duration / 8);
            var fileSizeFormattedString = StringService.ConvertBytesToFormattedString(fileSize);
            DownloadButtonContent = $"Download [{fileSizeFormattedString}]";
            return fileSizeFormattedString;
        }
        else
        {
            var audioStreamInfo = manifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            var videoStreamInfo = VideoService.GetVideoOnlyStreamInfo(manifest, SelectedQuality);

            var audioBitsPerSecond = audioStreamInfo.Bitrate.BitsPerSecond;
            var videoBitsPerSecond = videoStreamInfo.Bitrate.BitsPerSecond;
            var bitsPerSecond = audioBitsPerSecond + videoBitsPerSecond;

            var duration = video.Duration.Value.TotalSeconds;
            var fileSize = (long)(bitsPerSecond * duration / 8);

            var fileSizeFormattedString = StringService.ConvertBytesToFormattedString(fileSize);
            DownloadButtonContent = $"Download [{fileSizeFormattedString}]";
            return fileSizeFormattedString;
        }
    }

    private async Task GetDownloadedVideos()
    {
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            DownloadedVideoCardViewModels.Clear();
            var enumerable = UnitOfWork.VideosRepository.Find(s => true);
            HasDownloadHistory = enumerable.Any();
            if (!HasDownloadHistory) return;
            foreach (var video in UnitOfWork.VideosRepository.GetAll())
            {
                // Save the image to a temporary file
                try
                {
                    DownloadedVideoCardViewModels.Add(new DownloadedVideoCardViewModel(
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
                catch (Exception)
                {
                    // ignored
                }
            }
        });
    }
}