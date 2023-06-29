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
using VideoLibrary;

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

    [ObservableProperty] private string? _selectedFormat;

    [ObservableProperty] private ObservableCollection<string> _qualities = new();

    [ObservableProperty] private string? _selectedQuality;

    private Video? _video;
    private IEnumerable<Video>? _videos;
    private CancellationTokenSource? _cancellationTokenSource;

    public MediaClippexViewModel()
    {
        Formats.Add("mp4");
        Formats.Add("mp3");
        Formats.Add("wav");
        Formats.Add("ogg");
        Formats.Add("webm");
        Formats.Add("flac");
        Formats.Add("m4a");
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

        try
        {
            _video = await VideoService.GetVideo(Url);
            _videos = await VideoService.GetAllVideos(Url);

            if (_video == null)
            {
                MessageBox.Show("Video not found.");
                return;
            }

            if (_videos == null)
            {
                MessageBox.Show("Videos not found.");
                return;
            }

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
                videoInfoCardViewModel.ImageUrl = $"https://img.youtube.com/vi/{extractVideoId}/hqdefault.jpg";
                videoInfoCardViewModel.Duration = StringService.ConvertToTimeFormat(_video.Info.LengthSeconds);
                videoInfoCardViewModel.Description = _video.Info.Author;
                IsResolved = true;
                ShowPreview = true;
            });
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
        IsResolved = false;
        var downloadsFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var filePath = Path.Combine(downloadsFolder, "Downloads", _video.FullName);

        _cancellationTokenSource = new CancellationTokenSource();

        Task.Run(async () =>
        {
            IsDownloading = true;
            await using var fileStream = new FileStream(filePath, FileMode.Create);
            var totalBytes = _video.GetBytesAsync().Result;
            var bytesWritten = 0;
            MaxProgress = totalBytes.Length;
            while (bytesWritten < MaxProgress)
            {
                _cancellationTokenSource.Token.ThrowIfCancellationRequested(); // Check for cancellation request

                fileStream.Write(totalBytes);
                bytesWritten += fileStream.ReadByte();

                var currentProgress = bytesWritten;

                CurrentProgress = currentProgress;
                ProgressInfo = $"{StringService.ConvertBytesToFormattedString(CurrentProgress)} / {StringService.ConvertBytesToFormattedString(MaxProgress)}";
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
}