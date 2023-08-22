using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaClippex.DB.Core;
using MediaClippex.Helpers;
using MediaClippex.MVVM.Model;
using MediaClippex.Services;
using Russkyc.DependencyInjection.Implementations;
using YoutubeExplode.Videos.Streams;

namespace MediaClippex.MVVM.ViewModel;

// ReSharper disable once ClassNeverInstantiated.Global
// I do not know if this is good practice or not, did not even test it yet.
// pausing and resuming will not work yet, because it does not know how to do so.
public partial class QueuingContentCardViewModel : BaseViewModel
{
    private readonly string _url;
    private CancellationTokenSource _cancellationTokenSource = null!;
    [ObservableProperty] private string _duration;
    [ObservableProperty] private string? _fileType;
    [ObservableProperty] private bool _isProcessing;
    [ObservableProperty] private bool _paused;
    [ObservableProperty] private double _progress;
    [ObservableProperty] private string? _progressInfo;
    private string _selectedQuality;
    [ObservableProperty] private string _thumbnailUrl;
    [ObservableProperty] private string _title;


    public QueuingContentCardViewModel(string title, string duration, string thumbnailUrl, string url,
        string selectedQuality, bool newDownload = true, bool isAudio = false)
    {
        Title = title;
        Duration = duration;
        ThumbnailUrl = thumbnailUrl;
        _url = url;
        _selectedQuality = selectedQuality;
        IsProcessing = true;
        UnitOfWork = BuilderServices.Resolve<IUnitOfWork>();
        Task.Run(() =>
        {
            FileType = isAudio ? "Audio" : "Video";
            return newDownload ? DownloadProcess(isAudio) : SetPaused();
        });
    }

    private IUnitOfWork UnitOfWork { get; }

    // TODO: implement pausing of download
    private Task SetPaused()
    {
        var queuingVideos = UnitOfWork.QueuingContentRepository.Find(e => e.Title.Equals(Title));
        var firstOrDefault = queuingVideos.First();
        Progress = firstOrDefault.Progress;
        ProgressInfo = firstOrDefault.ProgressInfo;
        IsProcessing = false;
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void PauseDownload()
    {
        if (!Paused)
        {
            Task.Run(() =>
            {
                UnitOfWork.QueuingContentRepository.Find(e => e.Title.Equals(Title))
                    .First()
                    .Paused = true;
                Paused = UnitOfWork.Complete() == 1;
                ProgressInfo = Paused ? "Paused" : "In Progress";
            });
        }
    }

    [RelayCommand]
    private void CancelDownload()
    {
        var messageBoxResult = MessageBox.Show("Do you want to cancel the download?", "Cancel Download",
            MessageBoxButton.YesNo);
        if (messageBoxResult != MessageBoxResult.Yes) return;
        BuilderServices.Resolve<StorageService>().RemoveFromQueue(Title);
        _cancellationTokenSource.Cancel();
    }

    private async Task DownloadProcess(bool isAudio)
    {
        string savedPath;
        var fixedFileName = $"{StringService.FixFileName(Title)}";
        _cancellationTokenSource = new CancellationTokenSource();

        var videoFilePath = Path.Combine(DirectoryHelper.GetVideoSavingDirectory(), fixedFileName);
        var audioFilePath = Path.Combine(DirectoryHelper.GetAudioSavingDirectory(), fixedFileName);
        ProgressInfo = "Downloading";
        IsProcessing = true;
        var progressHandler = new Progress<double>(p =>
        {
            Progress = p * 100;
            switch (Progress)
            {
                case 60d:
                    CancelDownloadCommand.CanExecute(false);
                    break;
                case 80d:
                    ProgressInfo = "Almost finished";
                    break;
            }
        });

        UnitOfWork.QueuingContentRepository.Add(
            new QueuingVideo(Title, Duration, ThumbnailUrl, isAudio ? "Audio" : "Video", _url, Progress, ProgressInfo,
                _selectedQuality, Paused, isAudio)
        );

        UnitOfWork.Complete();
        var streamManifest = await VideoService.GetManifest(_url);

        if (isAudio)
        {
            var audioStreamInfo = VideoService.GetAudioOnlyStream(streamManifest, _selectedQuality);

            await VideoService.DownloadAudioOnly(audioStreamInfo, audioFilePath, progressHandler,
                _cancellationTokenSource.Token);
            savedPath = $"{audioFilePath}.mp3";
        }
        else
        {
            var audioStreamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            var videoStreamInfo = VideoService.GetVideoOnlyStreamInfo(streamManifest, _selectedQuality);
            await VideoService.DownloadMuxed(audioStreamInfo, videoStreamInfo, videoFilePath, progressHandler,
                _cancellationTokenSource.Token);
            savedPath = $"{videoFilePath}.{videoStreamInfo.Container}";
        }

        ProgressInfo = "Done";
        BuilderServices.Resolve<StorageService>().RemoveFromQueue(Title);
        var videoData = new Video(
            ThumbnailUrl,
            Title,
            Duration,
            "Add description here",
            isAudio ? "Audio" : "Video",
            isAudio
                ? VideoService.GetAudioFileSizeFormatted(streamManifest, await VideoService.GetVideo(_url),
                    _selectedQuality)
                : VideoService.GetVideoFileSizeFormatted(streamManifest, await VideoService.GetVideo(_url),
                    _selectedQuality),
            savedPath
        );

        UnitOfWork.VideosRepository.Add(videoData);

        var complete = UnitOfWork.Complete();
        MessageBox.Show($"Downloaded video added to db {complete == 1}");

        if (complete == 0) return;
        _selectedQuality = "";
        isAudio = false;
        MessageBox.Show(isAudio
            ? $"Audio downloaded successfully. Saved to {audioFilePath}"
            : $"Video downloaded successfully. Saved to {videoFilePath}");
        var mediaClippexViewModel = BuilderServices.Resolve<MediaClippexViewModel>();
        StorageService.CheckQueue(mediaClippexViewModel);
        StorageService.CheckDownloadHistory(mediaClippexViewModel);
    }
}