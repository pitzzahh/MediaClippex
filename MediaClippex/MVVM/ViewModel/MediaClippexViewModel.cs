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

    [ObservableProperty] private long _currentProgress;

    [ObservableProperty] private long _maxProgress;

    [ObservableProperty] private string? _quality = "Quality";

    [ObservableProperty] private string? _imagePreview;

    [ObservableProperty] private bool _isResolved;

    [ObservableProperty] private ObservableCollection<string> _formats = new();

    [ObservableProperty] private string _selectedFormat;

    [ObservableProperty] private ObservableCollection<string> _qualities = new();

    [ObservableProperty] private string? _selectedQuality;

    private List<string> _videoQualities = new();
    
    private List<string> _audioQualities = new();

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

                videoInfoCardViewModel.ImageUrl = _video.Thumbnails[0].Url;
                videoInfoCardViewModel.Duration =
                    StringService.ConvertToTimeFormat(_video.Duration.GetValueOrDefault());
                videoInfoCardViewModel.Description = _video.Description;
                IsResolved = true;
                ShowPreview = true;
            });
            InitializeVideoResolutions(manifest);
            InitializeAudioResolutions(manifest);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
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
        IsResolved = false;
        var userPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var filePath = Path.Combine(userPath, $"{StringService.FixFileName(_video.Title)}");

        _cancellationTokenSource = new CancellationTokenSource();

        if (string.IsNullOrWhiteSpace(Url)) return;

        Task.Run(async () =>
        {
            IsDownloading = true;
            if (IsAudioOnly)
            {
                await VideoService.DownloadAudioOnly(filePath, Url, SelectedQuality);
            }
            else
            {
                await VideoService.DownloadMuxed(filePath, Url, SelectedQuality);
            }
            
        }, _cancellationTokenSource.Token).ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                // Clean up resources if download was canceled
                IsDownloading = false;
                ShowPreview = false;
                File.Delete(filePath);
                MessageBox.Show("Download canceled.");
            }
            else if (task.IsFaulted)
            {
                // Handle any exceptions that occurred during the download
                IsDownloading = false;
                ShowPreview = false;
                MessageBox.Show("An error occurred during download: " + task.Exception?.Message);
            }
            else
            {
                // Download completed successfully
                IsDownloading = false;
                ShowPreview = false;
                MessageBox.Show("File Downloaded!");
            }
        }, TaskScheduler.FromCurrentSynchronizationContext());
    }


    [RelayCommand]
    private void CancelDownload()
    {
        _cancellationTokenSource?.Cancel();
    }

    private void InitializeVideoResolutions(StreamManifest manifest)
    {
        if (string.IsNullOrWhiteSpace(Url)) return;
        _audioQualities.Clear();
        manifest.GetVideoOnlyStreams()
            .Distinct()
            .ToList()
            .ForEach(s => _videoQualities.Add(s.VideoQuality.Label));
        _videoQualities.ForEach(q => Qualities.Add(q));
        SelectedQuality = Qualities.First();
    }

    private void InitializeAudioResolutions(StreamManifest manifest)
    {
        if (string.IsNullOrWhiteSpace(Url)) return;
        _audioQualities.Clear();
        manifest.GetAudioOnlyStreams()
            .Distinct()
            .ToList()
            .ForEach(s => _audioQualities.Add(s.Bitrate.ToString()));
    }
}