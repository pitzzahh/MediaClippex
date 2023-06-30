using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaClippex.Services;
using Russkyc.DependencyInjection.Implementations;
using YoutubeExplode.Common;
using YoutubeExplode.Videos;
using YoutubeExplode.Videos.Streams;

namespace MediaClippex.MVVM.ViewModel;

// ReSharper disable once ClassNeverInstantiated.Global
public partial class MediaClippexViewModel : BaseViewModel
{
    [ObservableProperty] [Required(ErrorMessage = "Please enter a URL.")]
    private string? _url;

    [ObservableProperty] private string? _status;

    [ObservableProperty] private bool _showPreview;

    [ObservableProperty] private bool _isDownloading;

    [ObservableProperty] private string? _progressInfo;

    [ObservableProperty] private double _progress;
    
    [ObservableProperty] private bool _isProgressIndeterminate;
    
    [ObservableProperty] private bool _isProcessing;

    [ObservableProperty] private string? _quality = "Quality";

    [ObservableProperty] private string? _imagePreview;

    [ObservableProperty] private bool _isResolved;

    [ObservableProperty] private ObservableCollection<string> _formats = new();

    [ObservableProperty] private string _selectedFormat;

    [ObservableProperty] private ObservableCollection<string> _qualities = new();

    [ObservableProperty] private string? _selectedQuality;

    private readonly List<string> _videoQualities = new();

    private readonly List<string> _audioQualities = new();

    private bool _isAudioOnly;

    public bool IsAudioOnly
    {
        get => _isAudioOnly;
        set
        {
            _isAudioOnly = value;
            Qualities.Clear();
            if (_isAudioOnly)
            {
                _audioQualities.ForEach(q => Qualities.Add(q));
            }
            else
            {
                _videoQualities.ForEach(q => Qualities.Add(q));
            }

            SelectedQuality = Qualities.First();
        }
    }

    private Video? _video;
    private CancellationTokenSource? _cancellationTokenSource;

    public MediaClippexViewModel()
    {
        Formats.Add("mp4");
        Formats.Add("mp3");
        SelectedFormat = Formats.First();
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
                var extractVideoId = StringService.ExtractVideoId(Url);
                if (extractVideoId.Equals(""))
                {
                    MessageBox.Show("Could not extract video ID.");
                    return;
                }

                videoInfoCardViewModel.ImageUrl = _video.Thumbnails.GetWithHighestResolution().Url;
                videoInfoCardViewModel.Duration = StringService.ConvertToTimeFormat(_video.Duration.GetValueOrDefault());
                videoInfoCardViewModel.Description = _video.Description;
            });
            InitializeVideoResolutions(manifest);
            InitializeAudioResolutions(manifest);
        }
        finally
        {
            ProgressInfo = "";
            IsProgressIndeterminate = false;
            IsResolved = true;
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

        _cancellationTokenSource = new CancellationTokenSource();

        if (string.IsNullOrWhiteSpace(Url)) return;

        ProgressInfo = "Downloading...";
        IsDownloading = true;
        Progress = 0;
        IsProcessing = true;
        
        try
        {
            if (IsAudioOnly)
            {
                await VideoService.DownloadAudioOnly(filePath, Url, SelectedQuality, new Progress<double>(p => Progress += p));
            }
            else
            {
                await VideoService.DownloadMuxed(filePath, Url, SelectedQuality, new Progress<double>(p => Progress += p));
            }
        }
        finally
        {
            ProgressInfo = "";
            IsDownloading = false;
            IsProcessing = false;
            ShowPreview = false;
            Progress = 0;
            IsResolved = false;
            MessageBox.Show("Download completed.");
        }
    }


    [RelayCommand]
    private void CancelDownload()
    {
        _cancellationTokenSource?.Cancel();
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
}