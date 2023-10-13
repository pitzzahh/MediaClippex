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
    private readonly CancellationTokenSource _cancellationTokenSource = null!;
    private readonly string _selectedQuality;
    private readonly string _url;
    [ObservableProperty] private string _duration;
    [ObservableProperty] private string _fileType;
    [ObservableProperty] private bool _isProcessing;
    [ObservableProperty] private bool _paused;
    [ObservableProperty] private double _progress;
    [ObservableProperty] private string? _progressInfo;
    [ObservableProperty] private string _thumbnailUrl;
    [ObservableProperty] private string _title;

    public QueuingContentCardViewModel(string title, string duration, string thumbnailUrl, string url,
        string selectedQuality, bool newDownload = true!, bool isAudio = false)
    {
        Title = title;
        Duration = duration;
        ThumbnailUrl = thumbnailUrl;
        _url = url;
        _selectedQuality = selectedQuality;
        IsProcessing = true;
        FileType = isAudio ? "Audio" : "Video";
        if (newDownload) _cancellationTokenSource = new CancellationTokenSource();
        UnitOfWork = BuilderServices.Resolve<IUnitOfWork>();
        Task.Run(() => newDownload ? DownloadProcess(isAudio) : SetPaused());
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
        var fixedFileName = $"{StringService.FixFileName(Title)}";

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

        try
        {
            // Starting of download
            var streamManifest = await VideoService.GetVideoManifest(_url);

            var savedPath = await InternalDownloadProcess(isAudio, streamManifest, audioFilePath, progressHandler,
                videoFilePath);

            ProgressInfo = "Done";
            BuilderServices.Resolve<StorageService>().RemoveFromQueue(Title);

            UnitOfWork.VideosRepository.Add(new Video(
                ThumbnailUrl,
                Title,
                Duration,
                FileType,
                savedPath
            ));

            var downloadedContentAdded = UnitOfWork.Complete();
            if (downloadedContentAdded == 0) return;

            var mediaClippexViewModel = BuilderServices.Resolve<MediaClippexViewModel>();
            mediaClippexViewModel.HasQueue = mediaClippexViewModel.QueuingContentCardViewModels.Count > 0;
            await mediaClippexViewModel.GetDownloadedVideos();
        }
        catch (Exception e)
        {
            MessageBox.Show($"Something went wrong while downloading internally: {e.Message}");
        }
    }

    private async Task<string> InternalDownloadProcess(bool isAudio, StreamManifest streamManifest,
        string audioFilePath,
        Progress<double> progressHandler, string videoFilePath)
    {
        string savedPath;
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

        return savedPath;
    }
}