using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

    [ObservableProperty] private string? _status;

    [ObservableProperty] private ObservableCollection<string> _themes = new();

    [ObservableProperty] [Required(ErrorMessage = "Please enter a URL.")]
    private string? _url;

    private Video? _video;
    public static Window UpdateWindow = new CheckUpdateView();

    public MediaClippexViewModel()
    {
        ThemeManager.Instance
            .GetColorThemes()
            .ToList()
            .ForEach(Themes.Add);
    }

    public string SelectedQuality
    {
        get => _selectedQuality;
        set
        {
            _selectedQuality = value;
            GetDownloadSize();
            OnPropertyChanging();
            OnPropertyChanged();
            OnPropertyChanged(SelectedQuality);
            OnPropertyChanged(DownloadButtonContent);
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
            OnPropertyChanged(SelectedQuality);
            OnPropertyChanged(DownloadButtonContent);
            InitializeAudioResolutions(manifest);
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

        var userPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var filePath = Path.Combine(userPath, "Downloads", $"{StringService.FixFileName(_video.Title)}");

        if (string.IsNullOrWhiteSpace(Url)) return;

        ProgressInfo = "Downloading...";
        IsDownloading = true;
        Progress = 0;
        IsProcessing = true;

        try
        {
            var progressHandler = new Progress<double>(p => Progress = p * 100);
            var manifest = await VideoService.GetManifest(Url);

            if (IsAudioOnly)
            {
                var audioStreamInfo = VideoService.GetAudioOnlyStream(manifest, SelectedQuality);

                await VideoService.DownloadAudioOnly(audioStreamInfo, filePath, progressHandler);
            }
            else
            {
                var audioStreamInfo = manifest.GetAudioOnlyStreams().GetWithHighestBitrate();
                var videoStreamInfo = VideoService.GetVideoOnlyStreamInfo(manifest, SelectedQuality);
                await VideoService.DownloadMuxed(audioStreamInfo, videoStreamInfo, filePath, progressHandler);
            }

            MessageBox.Show("Download completed. Saved to Downloads folder.");
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
    private static void CancelDownload()
    {
        MessageBox.Show("This feature is not implemented yet.");
    }

    private void InitializeVideoResolutions(StreamManifest manifest)
    {
        if (string.IsNullOrWhiteSpace(Url)) return;
        _videoQualities.Clear();
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
        Task.Run(async () =>
        {
            if (string.IsNullOrEmpty(Url)) return;
            var manifest = await VideoService.GetManifest(Url);
            var video = await VideoService.GetVideo(Url);

            if (video.Duration == null) return;

            if (IsAudioOnly)
            {
                var audioStreamInfo = VideoService.GetAudioOnlyStream(manifest, SelectedQuality);

                var bitsPerSecond = audioStreamInfo.Bitrate.BitsPerSecond;

                var duration = video.Duration.Value.TotalSeconds;
                var fileSize = (long)(bitsPerSecond * duration / 8);
                DownloadButtonContent =
                    $"Download [{StringService.ConvertBytesToFormattedString(fileSize)}]";
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

                DownloadButtonContent =
                    $"Download [{StringService.ConvertBytesToFormattedString(fileSize)}]";
            }
        });
    }
}