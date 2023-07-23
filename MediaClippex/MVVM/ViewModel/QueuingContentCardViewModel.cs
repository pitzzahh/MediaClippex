using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediaClippex.Helpers;
using MediaClippex.MVVM.Model;
using MediaClippex.Services;
using YoutubeExplode.Videos.Streams;

namespace MediaClippex.MVVM.ViewModel;

// ReSharper disable once ClassNeverInstantiated.Global
// I do not know if this is good practice or not, did not even test it yet.
// pausing and resuming will not work yet, because it does not know how to do so.
public partial class QueuingContentCardViewModel : BaseViewModel
{
    [ObservableProperty] private string _title;
    [ObservableProperty] private string _duration;
    [ObservableProperty] private string _thumbnailUrl;
    [ObservableProperty] private double _progress;
    [ObservableProperty] private string? _progressInfo;
    [ObservableProperty] private bool _paused;
    private CancellationTokenSource _cancellationTokenSource = null!;
    private readonly string _url;
    private string _selectedQuality;

    public QueuingContentCardViewModel(string title, string duration, string thumbnailUrl, string url,
        string selectedQuality, bool newDownload = true, bool isAudio = false)
    {
        Title = title;
        Duration = duration;
        ThumbnailUrl = thumbnailUrl;
        _url = url;
        _selectedQuality = selectedQuality;
        Task.Run(() => newDownload ? DownloadProcess(isAudio) : SetPaused());
    }

    private Task SetPaused()
    {
        var queuingVideos = MediaClippexViewModel.UnitOfWork.QueuingContentRepository.Find(e => e.Title.Equals(Title));
        var firstOrDefault = queuingVideos.First();
        Progress = firstOrDefault.Progress;
        ProgressInfo = firstOrDefault.ProgressInfo;
        return Task.CompletedTask;
    }

    [RelayCommand]
    private void PauseDownload()
    {
        if (!Paused)
        {
            Task.Run(() =>
            {
                MediaClippexViewModel.UnitOfWork.QueuingContentRepository.Find(e => e.Title.Equals(Title))
                    .First()
                    .Paused = true;
                Paused = MediaClippexViewModel.UnitOfWork.Complete() == 1;
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
        var progressHandler = new Progress<double>(p =>
        {
            Progress = p * 100;
            if (Progress <= 80d)
            {
                ProgressInfo = "Almost finished";
            }
        });

        MediaClippexViewModel.UnitOfWork.QueuingContentRepository.Add(
            new QueuingVideo(Title, Duration, ThumbnailUrl, _url, Progress, ProgressInfo, _selectedQuality, Paused, isAudio)
        );

        MediaClippexViewModel.UnitOfWork.Complete();

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

        var videoData = new Video(
            ThumbnailUrl,
            Title,
            Duration,
            "Add description here",
            isAudio ? "Audio" : "Video",
            isAudio ? VideoService.GetAudioFileSizeFormatted(streamManifest, await VideoService.GetVideo(_url), _selectedQuality) : VideoService.GetVideoFileSizeFormatted(streamManifest, await VideoService.GetVideo(_url), _selectedQuality),
            savedPath
        );

        MediaClippexViewModel.UnitOfWork.VideosRepository.Add(videoData);

        var complete = MediaClippexViewModel.UnitOfWork.Complete();

        if (complete != 0)
        {
            _selectedQuality = "";
            isAudio = false;
            MessageBox.Show(isAudio
                ? $"Audio downloaded successfully. Saved to {audioFilePath}"
                : $"Video downloaded successfully. Saved to {videoFilePath}");
        }
    }

}